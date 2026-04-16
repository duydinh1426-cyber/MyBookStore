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

        public AuthRepository(DBContext db)
        {
            _db = db;
        }

        public async Task<bool> EmailExists(string email)
        {
            return await _db.Accounts.AnyAsync(a => a.Email == email);
        }

        public async Task<bool> UserNameExists(string username)
        {
            return await _db.Accounts.AnyAsync(a => a.Username == username);
        }

        public async Task<Account?> GetByUsername(string username)
        {
            return await _db.Accounts
                .Include(a => a.Customers)
                .Include(a => a.Admins)
                .FirstOrDefaultAsync(a => a.Username == username);
        }

        public async Task<Account?> GetByEmail(string email)
        {
            return await _db.Accounts
                .Include(a => a.Customers)
                .Include(a => a.Admins)
                .FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<Account?> GetById(int accountId)
        {
            return await _db.Accounts
                .Include(a => a.Customers)
                .Include(a => a.Admins)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<Account> CreateAccount(Account account)
        {
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();
            return account;
        }

        public async Task CreateCustomer(Customer customer)
        {
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
        }

        public async Task SaveChanges()
        {
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAccount(Account account)
        {
            _db.Accounts.Update(account);
            await _db.SaveChangesAsync();
        }
    }
}
