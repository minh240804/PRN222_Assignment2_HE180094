using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Presentation.Hubs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Presentation.Pages.NewsArticleManagement
{
    public class IndexModel : PageModel
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubContext;

        public IndexModel(
            INewsArticleService newsArticleService,
            IHttpContextAccessor httpContextAccessor,
            IHubContext<NotificationHub> hubContext)
        {
            _newsArticleService = newsArticleService;
            _httpContextAccessor = httpContextAccessor;
            _hubContext = hubContext;
        }

        public List<NewsArticle> Articles { get; set; } = new();
        public bool IsStaff => _httpContextAccessor.HttpContext?.Session.GetInt32("Role") == 1;

        // ====== Filters (Bind GET) ======
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TagId { get; set; }

        /// <summary>
        /// "", "published", "draft"
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        // Dropdown Tag
        public List<SelectListItem> TagOptions { get; set; } = new();

        public void OnGet()
        {
            // Lấy toàn bộ (bao gồm Category/Tags/CreatedBy nếu service đã include;
            // nếu chưa, bạn chỉnh service để include hoặc tự thêm sau)
            var all = _newsArticleService.GetAll();

            // Build TagOptions từ dữ liệu hiện có
            TagOptions = all.SelectMany(a => a.Tags ?? new List<Tag>())
                            .GroupBy(t => new { t.TagId, t.TagName })
                            .OrderBy(g => g.Key.TagName)
                            .Select(g => new SelectListItem
                            {
                                Value = g.Key.TagId.ToString(),
                                Text = g.Key.TagName
                            })
                            .ToList();

            // ====== Apply filters ======
            IEnumerable<NewsArticle> q = all;

            // Search theo Title (case-insensitive)
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim().ToLowerInvariant();
                q = q.Where(a => (a.NewsTitle ?? string.Empty).ToLowerInvariant().Contains(s));
            }

            // Filter theo Tag
            if (TagId.HasValue)
            {
                var tagId = TagId.Value;
                q = q.Where(a => (a.Tags != null) && a.Tags.Any(t => t.TagId == tagId));
            }

            // Filter theo Status
            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                if (StatusFilter.Equals("published", StringComparison.OrdinalIgnoreCase))
                    q = q.Where(a => a.NewsStatus);
                else if (StatusFilter.Equals("draft", StringComparison.OrdinalIgnoreCase))
                    q = q.Where(a => !a.NewsStatus);
            }

            // Nếu role Lecturer (2) chỉ xem bài Published (tuỳ yêu cầu nghiệp vụ)
            var role = _httpContextAccessor.HttpContext?.Session.GetInt32("Role");
            if (role == 2)
            {
                q = q.Where(a => a.NewsStatus);
            }

            // Sắp xếp mới nhất trước
            Articles = q.OrderByDescending(a => a.ModifiedDate ?? a.CreatedDate).ToList();
        }

        // Đổi về async & id kiểu int (theo view đang dùng NewsArticleId là số)
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (!IsStaff)
                return Unauthorized();

            var article = _newsArticleService.Get(id);
            if (article == null)
                return NotFound();

            try
            {
                // Thông báo SignalR trước khi xoá
                await _hubContext.Clients.All
                    .SendAsync("ArticleDeleted", id, article.NewsTitle);

                await _hubContext.Clients.All.SendAsync("UpdateDashboardCounts");

                _newsArticleService.Delete(id);
                TempData["SuccessMessage"] = "Article deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Delete failed: " + ex.Message;
            }

            return RedirectToPage(new
            {
                Search,
                TagId,
                StatusFilter
            });
        }
    }
}


