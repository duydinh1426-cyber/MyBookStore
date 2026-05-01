using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace WebAPI.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        public UserService(IUserRepository repo) => _repo = repo;

        public async Task<ServiceResult<object>> GetAllUsersAsync(string? keyword, int page, int pageSize)
        {
            var query = _repo.GetQuery();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(a =>
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
                    email = a.Email,
                    address = a.Customers.Select(c => c.Address).FirstOrDefault() ?? "",
                    isAdmin = false,
                    createdAt = a.CreatedAt
                })
                .ToListAsync();

            var data = new
            {
                data = items, 
                total = total,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                page = page
            };
            return ServiceResult<object>.Success(data);
        }

        public async Task<ServiceResult<object>> GetUserDetailAsync(int id)
        {
            var account = await _repo.GetDetailByIdAsync(id);
            if (account == null)
                return ServiceResult<object>.Failure("Không tìm thấy người dùng");

            var customer = account.Customers.FirstOrDefault();
            var totalOrders = customer?.Orders?.Count ?? 0;
            var totalSpent = customer?.Orders?.Sum(o => o.TotalCost) ?? 0;

            var data = new
            {
                userId = customer?.UserId,
                accountId = account.AccountId,
                name = customer?.Name ?? "",
                email = account.Email,
                address = customer?.Address ?? "",
                isAdmin = false,
                totalOrders = totalOrders,
                totalSpent = (decimal)totalSpent,
                createdAt = account.CreatedAt
            };

            return ServiceResult<object>.Success(data);
        }
    }
}