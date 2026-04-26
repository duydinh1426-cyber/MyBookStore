using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<object> GetAccountAsync(string? keyword, int page, int pageSize);
        Task<Account?> GetDetailByIdAsync(int accountId);
        Task<Account?> GetBasicByIdAsync(int accountId);
        Task<bool> SaveChangesAsync();
    }
}