using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;


namespace Presentation.Pages.CategoryManagement
{
    public class IndexModel : PageModel
    {
        private readonly ICategoryService _cats;
        private readonly IHubContext<NotificationHub> _hubContext;

        public IndexModel(ICategoryService cats, IHubContext<NotificationHub> hubContext)
        {
            _cats = cats;
            _hubContext = hubContext;
        }

        public IEnumerable<Category> Categories { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Search { get; set; }
        public bool? Status { get; set; }

        // [BindProperty] 
        [BindProperty]
        public Category Category { get; set; }

        // [TempData] để hiển thị thông báo
        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        private int? Role => HttpContext.Session.GetInt32("Role");
        private bool IsStaff => Role == 1;

        // Handler cho HTTP GET (thay thế action Index)
        public IActionResult OnGet(string? search, bool? active, int currentPage = 1, int size = 5)
        {
            if (!IsStaff) return Unauthorized();

            var q = _cats.GetAll(active);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(x => x.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase));

            // Gán giá trị cho properties (thay vì ViewBag)

            CurrentPage = currentPage;
            TotalPages = (int)Math.Ceiling(q.Count() / (double)size);
            Search = search;
            Status = active;

            Categories = q.Skip((currentPage - 1) * size).Take(size);

            // Hiển thị thông báo nếu có
            if (!string.IsNullOrEmpty(SuccessMessage))
                ViewData["Success"] = SuccessMessage;
            if (!string.IsNullOrEmpty(ErrorMessage))
                ViewData["Error"] = ErrorMessage;

            return Page();
        }
        
        public async Task<IActionResult> OnPostDelete(short id)
        {
            //Console.WriteLine("delete in index");
            if (!IsStaff) return Unauthorized();

            try
            {
                var category = _cats.Get(id);
                var categoryName = category?.CategoryName ?? $"ID {id}";
                
                var isSuccess = _cats.Delete(id);
                if (isSuccess)
                {
                    SuccessMessage = "Category deleted successfully.";
                    
                    // Notify dashboard
                    await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                    {
                        eventType = "delete",
                        entityType = "category",
                        message = $"Category deleted: {categoryName}",
                        timestamp = DateTime.Now
                    });
                    // notify all staff to reload category list
                    await _hubContext.Clients.All.SendAsync("ReloadCategoryList");

                    await _hubContext.Clients.Group("Staff").SendAsync("ReceiveCreateCategoryNotification",
                        $"A category has been deleted");
                }
                else
                {
                    ErrorMessage = "Cannot delete this category because it is in use.";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "An error occurred while deleting the category.";
            }

            return RedirectToPage();
        }
    }
}