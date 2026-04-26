using MyBookStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Data.Repositories.Interfaces;
using Data.Models;

namespace Data.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DBContext _db;

        public AuthRepository(DBContext db) => _db = db;

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _db.Accounts.AnyAsync(e => e.Email == email);
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _db.Accounts
                .Include(c => c.Customers)
                .Include(a => a.Admins)
                .FirstOrDefaultAsync(e => e.Email == email);
        }
        

        public async Task<Account?> GetByIdAsync(int accountId)
        {
            return await _db.Accounts
                .Include (c => c.Customers)
                .Include (a => a.Admins)
                .FirstOrDefaultAsync (a => a.AccountId == accountId);
        }

        public void AddAccount(Account account) => _db.Accounts.Add(account);

        public void AddCustomer(Customer customer) => _db.Customers.Add(customer);

        public void UpdateAccount(Account account) => _db.Accounts.Update(account);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}
