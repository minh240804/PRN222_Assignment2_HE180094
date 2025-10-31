using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;

namespace Presentation.Pages.AccountManagement
{
    public class LoginModel : PageModel
    {
        private readonly IAccountService _acc;

        public LoginModel(IAccountService acc)
        {
            _acc = acc;
            Email = string.Empty;
            Password = string.Empty;
            ErrorMessage = string.Empty;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            var account = _acc.Login(Email, Password);
            if (account == null)
            {
                ErrorMessage = "Invalid email or password";
                return Page();
            }

            HttpContext.Session.SetInt32("AccountId", account.AccountId);
            HttpContext.Session.SetInt32("Role", account.AccountRole.GetValueOrDefault());
            HttpContext.Session.SetString("Name", account.AccountName ?? "User");
            
            if (account.AccountRole == 0)
                return RedirectToPage("Index");
            return RedirectToPage("/NewsArticleManagement/Index");
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }
    }
}