using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Data.Models;

namespace Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DBContext _db;
        public UserRepository(DBContext db) => _db = db;

        public async Task<object> GetAccountAsync
            (string? keyword, int page, int pageSize)
        {
            var query = _db.Accounts
                .Where(a => a.IsAdmin == false);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(a =>
                    (a.Email ?? "").Contains(kw) ||
                     a.Customers.Any(c => (c.Name ?? "").Contains(kw) ||
                                          (c.Address ?? "").Contains(kw))
                );
            }

            var total = await query.CountAsync();   

            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            var data = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    accountId = a.AccountId,
                    email = a.Email,
                    isAdmin = a.IsAdmin,
                    createdAt = a.CreatedAt,

                    customer = a.Customers
                        .Select(c => new
                        {
                            userId = c.UserId,
                            name = c.Name,
                            address = c.Address,
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new
            {
                total,
                page,
                pageSize,
                totalPages,
                data = data.Select(a => new
                {
                    accountId = a.accountId,
                    email = a.email,
                    isAdmin = a.isAdmin,
                    createdAt = a.createdAt,

                    userId = a.customer != null ? a.customer.userId : 0,
                    name = a.customer?.name ?? "",
                    address = a.customer?.address ?? ""
                })
            };
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