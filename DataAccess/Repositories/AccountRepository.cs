using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.DAO;

namespace Assignment2.DataAccess.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AccountDAO _dao;

        public AccountRepository(AccountDAO dao) => _dao = dao;

        public IEnumerable<SystemAccount> GetAll(int? role = null)
        {
            var accounts = _dao.GetAllAccounts();
            var q = accounts.AsQueryable();

            if (role.HasValue)
                q = q.Where(a => a.AccountRole == role);

            return q.OrderBy(a => a.AccountName).ToList();
        }

        public SystemAccount? Get(short id) => _dao.GetById(id);

        public SystemAccount? GetByEmail(string email) => _dao.GetByEmail(email);

        public short GetNextId()
        {
            var max = _dao.GetMaxId();
            return (short)(max + 1);
        }

        public void Add(SystemAccount acc)
        {
            if (acc.AccountId == 0)
                acc.AccountId = GetNextId();

            _dao.Add(acc);
        }

        public void Update(SystemAccount acc)
        {
            _dao.Update(acc);
        }

        public void Delete(SystemAccount acc)
        {
            _dao.Delete(acc);
        }
    }
}