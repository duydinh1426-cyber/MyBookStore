using MyBookStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Metadata.Ecma335;
using Data.Repositories.Interfaces;

namespace Data.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DBContext _db;

        public AuthRepository(DBContext db) => _db = db;

        public async Task<bool> IsEmailExistsAsync(string email)
            => await _db.Accounts.AnyAsync(a => a.Email == email);

        public async Task<Account?> GetByEmailAsync(string email)
            => await _db.Accounts
                        .Include(a => a.Customers)
                        .Include(a => a.Admins)
                        .FirstOrDefaultAsync(a => a.Email == email);
        

        public async Task<Account?> GetByIdAsync(int accountId)
            => await _db.Accounts
                        .Include(a => a.Customers)
                        .Include(a => a.Admins)
                        .FirstOrDefaultAsync(a => a.AccountId == accountId);

        public void AddAccount(Account account) => _db.Accounts.Add(account);
        public void AddCustomer(Customer customer) => _db.Customers.Add(customer);
        public void UpdateAccount(Account account) => _db.Accounts.Update(account);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}
