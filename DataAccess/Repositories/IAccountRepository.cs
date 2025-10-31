using System.Collections.Generic;
using Assignment2.DataAccess.Models;

namespace Assignment2.DataAccess.Repositories
{
    public interface IAccountRepository
    {
        IEnumerable<SystemAccount> GetAll(int? role = null);
        SystemAccount? Get(short id);
        SystemAccount? GetByEmail(string email);
        void Add(SystemAccount acc);
        void Update(SystemAccount acc);
        void Delete(SystemAccount acc);
        public short GetNextId();
    }
}