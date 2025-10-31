using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Presentation.Hubs;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Presentation.Pages.NewsArticleManagement
{
    public class FormModel : PageModel
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IAccountService _accountService;

        public FormModel(
            INewsArticleService newsArticleService,
            ICategoryService categoryService,
            ITagService tagService,
            IHttpContextAccessor httpContextAccessor,
            IHubContext<NotificationHub> hubContext,
            IAccountService accountService)
        {
            _newsArticleService = newsArticleService;
            _categoryService = categoryService;
            _tagService = tagService;
            _httpContextAccessor = httpContextAccessor;
            _hubContext = hubContext;
            _accountService = accountService;
        }

        [BindProperty]
        public Assignment2.DataAccess.Models.NewsArticle Article { get; set; } = default!;

        [BindProperty]
        public int[] SelectedTags { get; set; } = Array.Empty<int>();

        public IList<Category> Categories { get; set; } = default!;
        public IList<Tag> Tags { get; set; } = default!;

        public bool IsModal { get; set; }

        public bool IsCreate => string.IsNullOrEmpty(Article?.NewsArticleId);
        public bool IsStaff => _httpContextAccessor.HttpContext?.Session.GetInt32("Role") == 1;
        public int CurrentUserId => _httpContextAccessor.HttpContext?.Session.GetInt32("AccountId") ?? 0;

        public IActionResult OnGet(string? id)
        {
            LoadLookupData();

            if (!string.IsNullOrEmpty(id))
            {
                Article = _newsArticleService.Get(id);
                if (Article == null)
                    return NotFound();

                if (!IsStaff && Article.CreatedById != CurrentUserId)
                    return Unauthorized();

                SelectedTags = Article.Tags.Select(t => t.TagId).ToArray();
            }
            else
            {
                Article = new Assignment2.DataAccess.Models.NewsArticle
                {
                    CreatedById = (short)CurrentUserId,
                    NewsStatus = false 
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            bool isNew = string.IsNullOrEmpty(Article.NewsArticleId);

            if (isNew)
            {
                Article.NewsArticleId = Guid.NewGuid().ToString();
                Article.CreatedById = (short)CurrentUserId;
                Article.CreatedDate = DateTime.Now;
                Article.ModifiedDate = DateTime.Now;



                // Non-staff không được publish khi tạo
                if (!IsStaff) Article.NewsStatus = false;

                _newsArticleService.Add(Article, SelectedTags ?? Array.Empty<int>());

                var a = _accountService.Get(Article.CreatedById.Value);

                // Notify dashboard about new article
                await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                {
                    eventType = "create",
                    entityType = "article",
                    message = $"New article created: {Article.NewsTitle} by {a.AccountName}",
                    timestamp = DateTime.Now
                });

                if (Article.NewsStatus)
                {
                    await _hubContext.Clients.All.SendAsync("NewArticlePublished",
                       $"New Article Published by {a.AccountName}");
                }
            }
            else
            {
                var existing = _newsArticleService.Get(Article.NewsArticleId);
                if (existing == null) return NotFound();

                if (!IsStaff && existing.CreatedById != CurrentUserId) return Unauthorized();

                bool wasPublished = existing.NewsStatus;

                // Cập nhật trường
                existing.NewsTitle = Article.NewsTitle;
                existing.Headline = Article.Headline;
                existing.NewsContent = Article.NewsContent;
                existing.CategoryId = Article.CategoryId;
                existing.ModifiedDate = DateTime.Now;
                existing.UpdatedById = (short)CurrentUserId;

                if (IsStaff)
                    existing.NewsStatus = Article.NewsStatus;
                else
                    existing.NewsStatus = false;

                _newsArticleService.Update(existing, SelectedTags ?? Array.Empty<int>());

                // Determine notification message based on publish status change
                string eventType;
                string message;
                
                if (!wasPublished && existing.NewsStatus)
                {
                    // Changed from draft to published
                    var authorName = existing.CreatedBy?.AccountName ?? "Unknown";
                    eventType = "publish";
                    message = $"Article published: \"{existing.NewsTitle}\" by {authorName}";
                }
                else
                {
                    // Regular update
                    eventType = "update";
                    message = $"Article updated: \"{existing.NewsTitle}\"";
                }

                // Send single notification to dashboard
                await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                {
                    eventType = eventType,
                    entityType = "article",
                    message = message,
                    timestamp = DateTime.Now
                });

                TempData["SuccessMessage"] = "News updated successfully.";
            }

            if (IsModal) return new JsonResult(new { success = true });
            return RedirectToPage("Index");
        }


        private void LoadLookupData()
        {
            Categories = _categoryService.GetAll().ToList();
            Tags = _tagService.GetAll().ToList();
        }
    }
}
