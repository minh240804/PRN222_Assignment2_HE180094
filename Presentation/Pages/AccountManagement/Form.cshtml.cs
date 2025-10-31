using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Presentation.Pages.AccountManagement
{
    // Dùng ValidateAntiForgeryToken thay vì Ignore
    [ValidateAntiForgeryToken]
    public class FormModel : PageModel
    {
        private readonly IAccountService _acc;
        private readonly IHubContext<NotificationHub> _hubContext;

        public FormModel(IAccountService acc, IHubContext<NotificationHub> hubContext)
        {
            _acc = acc;
            _hubContext = hubContext;
        }

        [BindProperty]
        public SystemAccount Account { get; set; } = default!;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public bool IsCreate { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsModal { get; set; }

        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") is int r && r == 0;

        public IActionResult OnGet(short? id)
        {
            if (!IsAdmin()) return Unauthorized();

            IsCreate = !id.HasValue;
            if (IsCreate)
            {
                Account = new SystemAccount();
            }
            else
            {
                var existingAccount = _acc.Get(id.Value);
                if (existingAccount == null) return NotFound();

                Account = new SystemAccount
                {
                    AccountId = existingAccount.AccountId,
                    AccountName = existingAccount.AccountName,
                    AccountEmail = existingAccount.AccountEmail,
                    AccountRole = existingAccount.AccountRole,
                    AccountPassword = existingAccount.AccountPassword,
                    AccountStatus = existingAccount.AccountStatus
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsAdmin()) return Unauthorized();

            IsCreate = Account.AccountId == 0;

            // Normalize
            Account.AccountName = (Account.AccountName ?? string.Empty).Trim();
            Account.AccountEmail = (Account.AccountEmail ?? string.Empty).Trim().ToLowerInvariant();
            Password = (Password ?? string.Empty).Trim();

            // Keep email unchanged on edit
            if (!IsCreate)
            {
                var existing = _acc.Get(Account.AccountId);
                if (existing == null) return NotFound();
                Account.AccountEmail = existing.AccountEmail;
            }

            ValidateAccount(IsCreate);

            // >>> Stop here if any validation errors (prevents processing) <<<
            if (!ModelState.IsValid)
            {
                // Khi dùng modal partial, trả Page() để render lại form + lỗi
                return Page();
            }

            try
            {
                if (IsCreate)
                {
                    _acc.Add(Account, Password);
                    //await _hubContext.Clients.Group("Admin").SendAsync("ReceiveNewAccountNotification",
                    //    $"Admin has added a new account: {Account.AccountName}");
                    
                    await _hubContext.Clients.Group("Staff").SendAsync("ReceiveNewAccountNotification",
                        $"Admin has added a new account: {Account.AccountName}");
                    
                    // Notify dashboard
                    await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                    {
                        eventType = "create",
                        entityType = "account",
                        message = $"New account created: {Account.AccountName}",
                        timestamp = DateTime.Now
                    });
                    
                    TempData["SuccessMessage"] = "Account created successfully.";
                }
                else
                {
                    var existing = _acc.Get(Account.AccountId);
                    if (existing == null) return NotFound();

                    existing.AccountName = Account.AccountName;
                    existing.AccountRole = Account.AccountRole;

                    if (!Account.AccountStatus && existing.AccountStatus)
                    {
                        await _hubContext.Clients.All
                            .SendAsync("AccountDeactivated", Account.AccountId.ToString());
                    }

                    existing.AccountStatus = Account.AccountStatus;
                    _acc.Update(existing);
                    
                    // Notify dashboard
                    await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                    {
                        eventType = "update",
                        entityType = "account",
                        message = $"Account updated: {Account.AccountName}",
                        timestamp = DateTime.Now
                    });
                    
                    TempData["SuccessMessage"] = "Account updated successfully.";
                }

                if (IsModal) return new JsonResult(new { success = true });
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
                return Page();
            }
        }

        private void ValidateAccount(bool isCreate)
        {
            // Email duplicate: chỉ check khi tạo mới (vì khi edit giữ nguyên email)
            if (isCreate && _acc.ExistsEmail(Account.AccountEmail))
            {
                ModelState.AddModelError(nameof(Account.AccountEmail), "Email already exists.");
            }

            // Name
            if (string.IsNullOrWhiteSpace(Account.AccountName))
            {
                ModelState.AddModelError(nameof(Account.AccountName), "Name is required.");
            }
            else
            {
                if (Account.AccountName.Length > 100)
                    ModelState.AddModelError(nameof(Account.AccountName), "Name must not exceed 100 characters.");

                var nameRegex = new Regex(@"^[\p{L}\p{M}0-9 .,'\-/_/()]+$");
                if (!nameRegex.IsMatch(Account.AccountName))
                    ModelState.AddModelError(nameof(Account.AccountName),
                        "Name may contain letters, digits, spaces, and common punctuation only.");
            }

            // Email
            if (string.IsNullOrWhiteSpace(Account.AccountEmail))
            {
                ModelState.AddModelError(nameof(Account.AccountEmail), "Email is required.");
            }
            else
            {
                var emailAttr = new EmailAddressAttribute();
                if (!emailAttr.IsValid(Account.AccountEmail))
                    ModelState.AddModelError(nameof(Account.AccountEmail), "Email format is invalid.");
                if (Account.AccountEmail.Length > 150)
                    ModelState.AddModelError(nameof(Account.AccountEmail), "Email must not exceed 150 characters.");
            }

            // Role: 1 or 2
            if (Account.AccountRole != 1 && Account.AccountRole != 2)
                ModelState.AddModelError(nameof(Account.AccountRole), "Please select a valid role (Staff or Lecturer).");

            if (isCreate)
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    ModelState.AddModelError(nameof(Password), "Password is required.");
                }
                else
                {
                    if (Password.Length < 6)
                        ModelState.AddModelError(nameof(Password), "Password must be at least 6 characters.");

                    var strong = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,100}$");
                    if (!strong.IsMatch(Password))
                        ModelState.AddModelError(nameof(Password),
                            "Use a stronger password with upper/lowercase letters and digits.");
                }
            }
        }
    }
}
