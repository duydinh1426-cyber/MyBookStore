using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IUserRepository
    {
        IQueryable<Account> GetQuery();
        Task<Account?> GetDetailByIdAsync(int accountId);
        Task<Account?> GetBasicByIdAsync(int accountId);
        Task<bool> SaveChangesAsync();
    }
}