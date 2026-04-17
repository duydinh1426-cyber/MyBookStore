using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;

namespace Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DBContext _db;
        public UserRepository(DBContext db) => _db = db;

        public IQueryable<Account> GetQuery()
        {
            // Chỉ lấy tài khoản không phải Admin (Customers)
            return _db.Accounts
                .Include(a => a.Customers)
                .Where(a => a.IsAdmin == false)
                .AsQueryable();
        }

        public async Task<Account?> GetDetailByIdAsync(int accountId)
        {
            return await _db.Accounts
                .Include(a => a.Customers)
                    .ThenInclude(c => c.Orders)
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.IsAdmin == false);
        }

        public async Task<Account?> GetBasicByIdAsync(int accountId)
        {
            return await _db.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.IsAdmin == false);
        }

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}