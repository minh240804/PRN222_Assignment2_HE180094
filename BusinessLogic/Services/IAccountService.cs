using System.Collections.Generic;
using Assignment2.DataAccess.Models;

namespace Assignment2.BusinessLogic
{
    public interface IAccountService
    {
        IEnumerable<SystemAccount> GetAll(int? role = null);
        SystemAccount? Get(short id);
        SystemAccount? Login(string email, string password);
        void Add(SystemAccount acc, string password);
        void Update(SystemAccount acc, string? newPassword = null);
        (bool Success, string Message) Delete(short id);
        bool ExistsEmail(string? email);
    }
}