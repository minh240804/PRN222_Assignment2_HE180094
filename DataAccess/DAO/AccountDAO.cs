using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Assignment2.DataAccess.DAO
{
    public class AccountDAO
    {
        private readonly FunewsManagementContext _ctx;

        public AccountDAO(FunewsManagementContext ctx) => _ctx = ctx;

        public IEnumerable<SystemAccount> GetAllAccounts() =>
            _ctx.SystemAccounts.AsEnumerable();

        public SystemAccount? GetById(short id) =>
            _ctx.SystemAccounts
                .Include(a => a.NewsArticles)
                .FirstOrDefault(a => a.AccountId == id);

        public SystemAccount? GetByEmail(string email) =>
            _ctx.SystemAccounts.FirstOrDefault(a => a.AccountEmail == email);

        public short GetMaxId() =>
            _ctx.SystemAccounts.Max(a => (short?)a.AccountId) ?? 0;

        public void Add(SystemAccount acc)
        {
            _ctx.SystemAccounts.Add(acc);
            _ctx.SaveChanges();
        }

        public void Update(SystemAccount acc)
        {
            _ctx.SystemAccounts.Update(acc);
            _ctx.SaveChanges();
        }

        public void Delete(SystemAccount acc)
        {
            _ctx.SystemAccounts.Remove(acc);
            _ctx.SaveChanges();
        }
    }
}