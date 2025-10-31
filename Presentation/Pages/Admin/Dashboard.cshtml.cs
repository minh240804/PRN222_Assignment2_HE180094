using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;

namespace Presentation.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IDashboardService _dashboardService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardModel(
            IDashboardService dashboardService,
            IHttpContextAccessor httpContextAccessor)
        {
            _dashboardService = dashboardService;
            _httpContextAccessor = httpContextAccessor;
        }

        public DashboardStats Stats { get; set; } = new DashboardStats();

        public IActionResult OnGet()
        {
            // Check if user is admin
            var role = _httpContextAccessor.HttpContext?.Session.GetInt32("Role");
            if (!role.HasValue || role.Value != 0)
            {
                return RedirectToPage("/Index");
            }

            Stats = _dashboardService.GetDashboardStats();
            return Page();
        }

        public IActionResult OnGetStats()
        {
            // API endpoint for real-time updates
            var stats = _dashboardService.GetDashboardStats();
            return new JsonResult(stats);
        }
    }
}
