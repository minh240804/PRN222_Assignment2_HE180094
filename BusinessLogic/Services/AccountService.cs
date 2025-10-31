using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.Repositories;
using Microsoft.Extensions.Options;

namespace Assignment2.BusinessLogic;

public class AccountService : IAccountService
{
    readonly IAccountRepository _repo;
    readonly AdminAccountOptions _opt;

    public AccountService(IAccountRepository repo, IOptions<AdminAccountOptions> opt)
    {
        _repo = repo;
        _opt = opt.Value;
    }

    public IEnumerable<SystemAccount> GetAll(int? role = null) =>
        _repo.GetAll(role);

    public SystemAccount? Get(short id) => _repo.Get(id);

    public SystemAccount? Login(string email, string password)
    {
        if (email.Equals(_opt.Email, System.StringComparison.OrdinalIgnoreCase)
            && password == _opt.Password)
            return new SystemAccount
            {
                AccountId = 0,
                AccountName = "Administrator",
                AccountEmail = _opt.Email,
                AccountRole = 0
            };

        var acc = _repo.GetByEmail(email);
        return acc != null && acc.AccountPassword == password ? acc : null;
    }

    public void Add(SystemAccount acc, string rawPassword)
    {
        acc.AccountPassword = rawPassword;
        _repo.Add(acc);
    }

    public void Update(SystemAccount acc, string? newPassword = null)
    {
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            acc.AccountPassword = newPassword;
        }
        _repo.Update(acc);
    }

    public (bool Success, string Message) Delete(short id)
    {
        var account = _repo.Get(id);
        if (account == null)
            return (false, "Account not found.");

        // Check if account has any articles
        if (account.NewsArticles != null && account.NewsArticles.Any())
        {
            var articleCount = account.NewsArticles.Count;
            return (false, $"Cannot delete account. This account has {articleCount} article(s) associated with it. Please reassign or delete the articles first.");
        }

        _repo.Delete(account);
        return (true, "Account deleted successfully.");
    }

    public bool ExistsEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && _repo.GetAll().Any(a => a.AccountEmail == email);
}
