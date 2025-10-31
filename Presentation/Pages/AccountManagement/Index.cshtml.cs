using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;

namespace Presentation.Pages.AccountManagement
{
    public class IndexModel : PageModel
    {
        private readonly IAccountService _acc;
        private readonly IHubContext<NotificationHub> _hub;

        public IndexModel(IAccountService acc, IHubContext<NotificationHub> hub)
        {
            _acc = acc;
            _hub = hub;
        }

        public async Task<IActionResult> OnPostDelete(short id)
        {
            if (!IsAdmin()) return Unauthorized();

            var account = _acc.Get(id);
            if (account == null) return NotFound();

            var accountName = account.AccountName; // Store name before deletion

            await _hub.Clients.All.SendAsync("AccountDeactivated", id.ToString());

            
            _acc.Delete(id);
            
            // Notify dashboard
            await _hub.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
            {
                eventType = "delete",
                entityType = "account",
                message = $"Account deleted: {accountName}",
                timestamp = DateTime.Now
            });
            
            TempData["SuccessMessage"] = "Account deleted successfully.";
            return RedirectToPage();
        }

        public List<SystemAccount> Accounts { get; set; } = new List<SystemAccount>();

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? RoleFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        public int TotalPages { get; set; }
        
        private const int PageSize = 5;

        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") is int r && r != 1 && r != 2;

        public IActionResult OnGet()
        {
            if (!IsAdmin()) return Unauthorized();

            var query = _acc.GetAll();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(x =>
                    (x.AccountName ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    (x.AccountEmail ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase));
            }

            if (RoleFilter.HasValue && RoleFilter > 0)
            {
                query = query.Where(x => x.AccountRole == RoleFilter.Value);
            }

            var total = query.Count();
            TotalPages = (int)Math.Ceiling((double)total / PageSize);

            Accounts = query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        
    }
}