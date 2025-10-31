using System.Text.RegularExpressions;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.AccountManagement
{
    [ValidateAntiForgeryToken]
    public class ProfileModel : PageModel
    {
        private readonly IAccountService _acc;
        public ProfileModel(IAccountService acc) => _acc = acc;

        [BindProperty] public SystemAccount Account { get; set; } = default!;
        [BindProperty] public string? OldPassword { get; set; }
        [BindProperty] public string? NewPassword { get; set; }
        [BindProperty] public string? ConfirmPassword { get; set; }

        public void OnGet()
        {
            var id = HttpContext.Session.GetInt32("AccountId");
            if (id is null) { Response.Redirect("/AccountManagement/Login"); return; }

            var acc = _acc.Get((short)id.Value);
            if (acc == null) { Account = new SystemAccount(); return; }

            Account = new SystemAccount
            {
                AccountId = acc.AccountId,
                AccountName = acc.AccountName,
                AccountEmail = acc.AccountEmail,
                AccountRole = acc.AccountRole,
                AccountStatus = acc.AccountStatus
            };
        }

        public IActionResult OnPost()
        {
            // Normalize
            Account.AccountName = (Account.AccountName ?? "").Trim();

            // Validate name
            if (string.IsNullOrWhiteSpace(Account.AccountName))
                ModelState.AddModelError(nameof(Account.AccountName), "Name is required.");
            else if (Account.AccountName.Length > 100)
                ModelState.AddModelError(nameof(Account.AccountName), "Name must not exceed 100 characters.");
            else
            {
                var nameRegex = new Regex(@"^[\p{L}\p{M}0-9 .,'\-_/()]+$");
                if (!nameRegex.IsMatch(Account.AccountName))
                    ModelState.AddModelError(nameof(Account.AccountName),
                        "Name may contain only letters, digits, spaces, and common punctuation.");
            }

            bool wantsChangePassword =
                !string.IsNullOrWhiteSpace(OldPassword) ||
                !string.IsNullOrWhiteSpace(NewPassword) ||
                !string.IsNullOrWhiteSpace(ConfirmPassword);

            if (wantsChangePassword)
            {
                // Admin (id=0, cấu hình) không cho đổi mật khẩu tại đây
                if (Account.AccountId == 0)
                {
                    ModelState.AddModelError(nameof(OldPassword),
                        "Administrator password is configured by the system and cannot be changed here.");
                }

                if (string.IsNullOrWhiteSpace(OldPassword))
                    ModelState.AddModelError(nameof(OldPassword), "Old password is required.");
                if (string.IsNullOrWhiteSpace(NewPassword))
                    ModelState.AddModelError(nameof(NewPassword), "New password is required.");
                if (string.IsNullOrWhiteSpace(ConfirmPassword))
                    ModelState.AddModelError(nameof(ConfirmPassword), "Please confirm the new password.");

                if (ModelState.IsValid)
                {
                    // Kiểm tra old password bằng Login(email, old)
                    var login = _acc.Login(Account.AccountEmail, OldPassword!);
                    if (login == null || login.AccountId != Account.AccountId)
                    {
                        ModelState.AddModelError(nameof(OldPassword), "Old password is incorrect.");
                    }
                    else
                    {
                        // Rule mạnh (min 6, có hoa, thường, số)
                        var strong = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,100}$");
                        if (!strong.IsMatch(NewPassword!))
                            ModelState.AddModelError(nameof(NewPassword),
                                "Use a stronger password with upper/lowercase letters and digits (min 6).");

                        if (OldPassword == NewPassword)
                            ModelState.AddModelError(nameof(NewPassword),
                                "New password must be different from the old password.");

                        if (!string.Equals(NewPassword, ConfirmPassword))
                            ModelState.AddModelError(nameof(ConfirmPassword),
                                "Confirm password does not match.");
                    }
                }
            }

            if (!ModelState.IsValid) return Page();

            var existing = _acc.Get(Account.AccountId);
            if (existing == null) return NotFound();

            // Cập nhật name
            existing.AccountName = Account.AccountName;

            // Đổi mật khẩu nếu có yêu cầu (chỉ dùng Update của service)
            if (wantsChangePassword && Account.AccountId != 0)
            {
                _acc.Update(existing, NewPassword); // service sẽ set AccountPassword = newPassword
            }
            else
            {
                _acc.Update(existing); // chỉ cập nhật name
            }

            TempData["Success"] = wantsChangePassword
                ? "Profile and password updated successfully."
                : "Profile updated successfully.";

            return RedirectToPage(); // PRG
        }
    }
}
