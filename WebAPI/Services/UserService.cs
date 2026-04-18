using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        public UserService(IUserRepository repo) => _repo = repo;

        public async Task<object> GetAllUsersAsync(string? keyword, int page, int pageSize)
        {
            var query = _repo.GetQuery();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(a =>
                    a.Username.ToLower().Contains(kw) ||
                    (a.Email ?? "").ToLower().Contains(kw) ||
                    a.Customers.Any(c => (c.Name ?? "").ToLower().Contains(kw) ||
                                         (c.Address != null && c.Address.ToLower().Contains(kw)))
                );
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new {
                    userId = a.Customers.Select(c => c.UserId).FirstOrDefault(),
                    accountId = a.AccountId,
                    name = a.Customers.Select(c => c.Name).FirstOrDefault() ?? "",
                    username = a.Username,
                    email = a.Email,
                    address = a.Customers.Select(c => c.Address).FirstOrDefault() ?? "",
                    isAdmin = false,
                    createdAt = a.CreatedAt
                })
                .ToListAsync();

            return new
            {
                data = items, 
                total = total,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                page = page
            };
        }

        public async Task<object?> GetUserDetailAsync(int id)
        {
            var account = await _repo.GetDetailByIdAsync(id);
            if (account == null) return null;

            var customer = account.Customers.FirstOrDefault();
            var totalOrders = customer?.Orders?.Count ?? 0;
            var totalSpent = customer?.Orders?.Sum(o => o.TotalCost) ?? 0;

            return new
            {
                userId = customer?.UserId,
                accountId = account.AccountId,
                name = customer?.Name ?? "",
                username = account.Username,
                email = account.Email,
                address = customer?.Address ?? "",
                isAdmin = false,
                totalOrders = totalOrders,
                totalSpent = (decimal)totalSpent,
                createdAt = account.CreatedAt
            };
        }

        public async Task<object> ResetPasswordAsync(int id)
        {
            var account = await _repo.GetBasicByIdAsync(id);
            if (account == null) return new { message = "NotFound" };

            const string DEFAULT_PASSWORD = "123456";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(DEFAULT_PASSWORD));
            account.Password = Convert.ToHexString(bytes).ToLower();

            if (await _repo.SaveChangesAsync())
                return new { message = "Đã reset mật khẩu về 123456." };

            return new { message = "Lỗi khi cập nhật mật khẩu." };
        }
    }
}