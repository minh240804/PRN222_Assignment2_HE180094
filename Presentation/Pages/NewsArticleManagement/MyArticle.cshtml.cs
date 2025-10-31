using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Pages.NewsArticleManagement
{
    public class MyArticleModel : PageModel
    {
        private readonly INewsArticleService _news;
        private readonly IHttpContextAccessor _http;
        private readonly IHubContext<Presentation.Hubs.NotificationHub>? _hub; // optional nếu bạn muốn bắn sự kiện

        public MyArticleModel(
            INewsArticleService newsArticleService,
            IHttpContextAccessor httpContextAccessor,
            IHubContext<Presentation.Hubs.NotificationHub>? hubContext = null)
        {
            _news = newsArticleService;
            _http = httpContextAccessor;
            _hub = hubContext;
        }

        public List<NewsArticle> Articles { get; set; } = new();

        // Filters
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        /// <summary>"", "published", "draft"</summary>
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public IActionResult OnGet()
        {
            var accountId = _http.HttpContext?.Session.GetInt32("AccountId");
            if (!accountId.HasValue)
            {
                return RedirectToPage("/AccountManagement/Login");
            }

            // Lấy tất cả rồi lọc theo chủ sở hữu
            var all = _news.GetAll(); // IEnumerable<NewsArticle>
            IEnumerable<NewsArticle> q = all;

            // Chỉ bài do chính user tạo (CreatedById giả định tồn tại; nếu không, so CreatedBy?.AccountId)
            q = q.Where(a =>
                (a.CreatedById == (short)accountId.Value) ||
                (a.CreatedBy != null && a.CreatedBy.AccountId == (short)accountId.Value)
            );

            // Search theo title
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim().ToLowerInvariant();
                q = q.Where(a => (a.NewsTitle ?? string.Empty).ToLowerInvariant().Contains(s));
            }

            // Filter theo status
            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                if (StatusFilter.Equals("published", StringComparison.OrdinalIgnoreCase))
                    q = q.Where(a => a.NewsStatus);
                else if (StatusFilter.Equals("draft", StringComparison.OrdinalIgnoreCase))
                    q = q.Where(a => !a.NewsStatus);
            }

            Articles = q
                .OrderByDescending(a => a.ModifiedDate ?? a.CreatedDate)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var accountId = _http.HttpContext?.Session.GetInt32("AccountId");
            var role = _http.HttpContext?.Session.GetInt32("Role");

            if (!accountId.HasValue)
                return Unauthorized();

            var article = _news.Get(id);
            if (article == null)
                return NotFound();

            // Chỉ cho xoá nếu là chủ bài hoặc Staff/Admin (role 1/0) – tuỳ nghiệp vụ
            var isOwner = (article.CreatedById == (short)accountId.Value) ||
                          (article.CreatedBy != null && article.CreatedBy.AccountId == (short)accountId.Value);

            var isStaffOrAdmin = role == 0 || role == 1;

            if (!isOwner && !isStaffOrAdmin)
                return Forbid();

            try
            {
                _news.Delete(id);

                // (tuỳ chọn) phát sự kiện SignalR như trang Index
                if (_hub != null)
                {
                    await _hub.Clients.Group("Admin").SendAsync("ArticleDeleted", id.ToString(), article.NewsTitle);
                    await _hub.Clients.Group("Staff").SendAsync("ArticleDeleted", id.ToString(), article.NewsTitle);
                    await _hub.Clients.All.SendAsync("UpdateDashboardCounts");
                }

                TempData["SuccessMessage"] = "Article deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Delete failed: " + ex.Message;
            }

            // Giữ lại filter hiện tại
            return RedirectToPage(new { Search, StatusFilter });
        }
    }
}
