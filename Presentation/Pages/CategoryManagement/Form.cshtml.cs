using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata;
using Presentation.Hubs;

namespace Presentation.Pages.CategoryManagement
{
    public class FormModel : PageModel
    {
        private readonly ICategoryService _cats;
        private readonly IHubContext<NotificationHub> _hubContext;

        public FormModel(ICategoryService cats, IHubContext<NotificationHub> hubContext)
        {
            _cats = cats;
            _hubContext = hubContext;
        }
        [BindProperty]
        public Category Category { get; set; }

        public List<SelectListItem> ParentCategories { get; set; } = new List<SelectListItem>();

        public bool IsCreate { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsModal { get; set; }

        private int? Role => HttpContext.Session.GetInt32("Role");
        private bool IsStaff => Role == 1;

        private void Validate(Category c)
        {
            //var search = _cats.
            if (string.IsNullOrWhiteSpace(c.CategoryName))
                ModelState.AddModelError(nameof(c.CategoryName), "Name is required");
            if (c.ParentCategoryId == c.CategoryId)
                ModelState.AddModelError(nameof(c.ParentCategoryId), "Parent cannot be itself");
            
        }

        public IActionResult OnGet(short? id)
        {
            if (!IsStaff) return Unauthorized();

            // Lấy tất cả danh mục (bao gồm cả con để chọn cha)
            var allCat = _cats.GetAll(true).ToList();

            IsCreate = !id.HasValue;

            if (id.HasValue)
            {
                var existingCategory = _cats.Get(id.Value);
                if (existingCategory == null)
                    return NotFound();

                Category = new Category
                {
                    CategoryId = existingCategory.CategoryId,
                    CategoryName = existingCategory.CategoryName,
                    CategoryDesciption = existingCategory.CategoryDesciption,
                    ParentCategoryId = existingCategory.ParentCategoryId,
                    IsActive = existingCategory.IsActive
                };
            }
            else
            {
                Category = new Category { IsActive = true };
            }

            // Tạo dropdown danh mục cha
            ParentCategories = allCat
                .Where(c => !id.HasValue || c.CategoryId != id.Value)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName,
                    Selected = Category.ParentCategoryId == c.CategoryId
                })
                .ToList();

            Console.WriteLine(IsModal);

            return Page();
        }

        private void LoadLookups(short? parentId = null) =>
            ViewData["ParentList"] = new SelectList(_cats.GetAll(true), "CategoryId", "CategoryName", parentId);

        public IActionResult OnPost()
        {
            Console.WriteLine("Post modal");
            if (!IsStaff) return Unauthorized();

            Validate(Category);

            var find = _cats.GetAll()
                .FirstOrDefault(c => c.CategoryName.Equals(Category.CategoryName, StringComparison.OrdinalIgnoreCase));
            if(find != null && Category.CategoryId == 0)
            {
                ModelState.AddModelError(nameof(Category.CategoryName), "Category name already exists.");
            }

            if (!ModelState.IsValid)
            {
                LoadLookups(Category.ParentCategoryId);
                //ShowModal = true;
                return Page();
            }

            if (Category.CategoryId == 0)
            {
                _cats.Add(Category);
                SuccessMessage = "Category created successfully.";
                //_hubContext.Clients.All.SendAsync("ReceiveCreateCategoryNotification",
                //    $"A new category has been created: {Category.CategoryName}");
                _hubContext.Clients.All.SendAsync("ReloadCategoryList");
                _hubContext.Clients.Group("Staff").SendAsync("ReceiveCreateCategoryNotification",
                    $"A new category has been created: {Category.CategoryName}");
                
                // Notify dashboard
                _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                {
                    eventType = "create",
                    entityType = "category",
                    message = $"New category created: {Category.CategoryName}",
                    timestamp = DateTime.Now
                });
            }
            else
            {
                _cats.Update(Category);
                SuccessMessage = "Category updated successfully.";
                var result = _cats.Update(Category);
                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    LoadLookups(Category.ParentCategoryId);
                    IsModal = true;
                    return Page();
                }
                //_hubContext.Clients.All.SendAsync("ReceiveCreateCategoryNotification",
                //    $"A category has been updated: {Category.CategoryName}");
                _hubContext.Clients.All.SendAsync("ReloadCategoryList");
                _hubContext.Clients.Group("Staff").SendAsync("ReceiveCreateCategoryNotification",
                    $"A new category has been created: {Category.CategoryName}");
                // Notify dashboard
                _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                {
                    eventType = "update",
                    entityType = "category",
                    message = $"Category updated: {Category.CategoryName}",
                    timestamp = DateTime.Now
                });
            }

            if (IsModal) return new JsonResult(new { success = true });
            return RedirectToPage("Index"); ;
        }
    }
}
