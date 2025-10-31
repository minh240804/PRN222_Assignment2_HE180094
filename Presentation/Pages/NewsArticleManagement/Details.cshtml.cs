using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Presentation.Hubs;

namespace Presentation.Pages.NewsArticleManagement
{
    [IgnoreAntiforgeryToken]
    public class DetailsModel : PageModel
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly IArticleCommentService _commentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubContext;

        public DetailsModel(
            INewsArticleService newsArticleService,
            IArticleCommentService commentService,
            IHttpContextAccessor httpContextAccessor,
            IHubContext<NotificationHub> hubContext)
        {
            _newsArticleService = newsArticleService;
            _commentService = commentService;
            _httpContextAccessor = httpContextAccessor;
            _hubContext = hubContext;
        }

        public NewsArticle? Article { get; set; }
        public List<NewsArticle> RelatedArticles { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public string CurrentUserName { get; set; } = "Guest";
        public bool IsLoggedIn { get; set; }
        public bool IsAdmin { get; set; }
        public short CurrentUserId { get; set; }

        public IActionResult OnGet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            Article = _newsArticleService.Get(id);

            if (Article == null)
            {
                return NotFound();
            }

            // Get related articles
            RelatedArticles = _newsArticleService
                .GetRelatedArticles(id, Article.CategoryId, 3)
                .ToList();

            // Load comments from database
            Comments = _commentService.GetByArticle(id).ToList();

            // Check if user is logged in and get their info
            var accountId = _httpContextAccessor.HttpContext?.Session.GetInt32("AccountId");
            var role = _httpContextAccessor.HttpContext?.Session.GetInt32("Role");

            IsLoggedIn = accountId.HasValue;
            IsAdmin = role.HasValue && role.Value == 0; // Role 0 is Admin
            CurrentUserId = (short)(accountId ?? 0);

            if (IsLoggedIn)
            {
                CurrentUserName = _httpContextAccessor.HttpContext?.Session.GetString("Name") ?? "User";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSendComment(string articleId, string message)
        {
            // Check if user is logged in
            var accountId = _httpContextAccessor.HttpContext?.Session.GetInt32("AccountId");
            var role = _httpContextAccessor.HttpContext?.Session.GetInt32("Role");

            if (!accountId.HasValue)
            {
                return new JsonResult(new { success = false, error = "You must be logged in to comment" })
                {
                    StatusCode = 401
                };
            }

            // Admins cannot comment - only moderate
            if (role.HasValue && role.Value == 0)
            {
                return new JsonResult(new { success = false, error = "Admins cannot post comments" })
                {
                    StatusCode = 403
                };
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new { success = false, error = "Message cannot be empty" });
            }

            var userName = _httpContextAccessor.HttpContext?.Session.GetString("Name") ?? "User";
            var timestamp = DateTime.Now;

            // Save comment to database and get the created comment with ID
            var newComment = _commentService.Add(articleId, (short)accountId.Value, message);

            // Send comment to all clients in the article group via SignalR
            await _hubContext.Clients
                .Group($"article_{articleId}")
                .SendAsync("ReceiveComment", new
                {
                    commentId = newComment.CommentId,
                    user = userName,
                    message,
                    timestamp = timestamp.ToString("HH:mm:ss dd/MM/yyyy")
                });

            // Notify dashboard about new comment
            await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
            {
                eventType = "create",
                entityType = "comment",
                message = $"New comment by {userName} on article {articleId}",
                timestamp = DateTime.Now
            });

            return new JsonResult(new
            {
                success = true,
                commentId = newComment.CommentId,
                user = userName,
                message,
                timestamp = timestamp.ToString("HH:mm:ss dd/MM/yyyy")
            });
        }

        public async Task<IActionResult> OnPostDeleteComment(int commentId, string articleId)
        {
            try
            {
                // Check if user is admin
                var accountId = _httpContextAccessor.HttpContext?.Session.GetInt32("AccountId");
                var role = _httpContextAccessor.HttpContext?.Session.GetInt32("Role");
                
                if (!accountId.HasValue || !role.HasValue || role.Value != 0)
                {
                    return new JsonResult(new { success = false, error = "Only admins can delete comments" })
                    {
                        StatusCode = 403
                    };
                }

                var comment = _commentService.Get(commentId);
                if (comment == null)
                {
                    return new JsonResult(new { success = false, error = "Comment not found" })
                    {
                        StatusCode = 404
                    };
                }

                var authorId = comment.AccountId;
                var adminName = _httpContextAccessor.HttpContext?.Session.GetString("Name") ?? "Admin";

                Console.WriteLine($"Admin attempting delete - AccountID: {accountId.Value}, Name: {adminName}");

                // Delete comment (soft delete)
                _commentService.Delete(commentId, (short)accountId.Value);

                // Notify the comment author via SignalR
                await _hubContext.Clients
                    .Group($"account_{authorId}")
                    .SendAsync("CommentDeleted", new
                    {
                        commentId,
                        articleId,
                        reason = $"Your comment was removed by {adminName} for violating community guidelines",
                        deletedBy = adminName,
                        timestamp = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")
                    });

                // Notify all users in the article group that the comment was removed
                await _hubContext.Clients
                    .Group($"article_{articleId}")
                    .SendAsync("CommentRemovedFromArticle", new
                    {
                        commentId,
                        message = "A comment was removed by moderator"
                    });

                // Notify dashboard about comment deletion
                await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                {
                    eventType = "delete",
                    entityType = "comment",
                    message = $"Comment #{commentId} deleted by {adminName}",
                    timestamp = DateTime.Now
                });

                return new JsonResult(new
                {
                    success = true,
                    message = "Comment deleted successfully",
                    commentId
                });
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error deleting comment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return new JsonResult(new
                {
                    success = false,
                    error = "An error occurred while deleting the comment",
                    details = ex.Message
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
