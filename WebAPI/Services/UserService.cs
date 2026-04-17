using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        public UserService(IUserRepository repo) => _repo = repo;

        public async Task<ApiResponse<object>> GetAllUsersAsync(string? keyword, int page, int pageSize)
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
                    AccountId = a.AccountId,
                    UserId = a.Customers.Select(c => c.UserId).FirstOrDefault(),
                    Username = a.Username,
                    Email = a.Email,
                    Name = a.Customers.Select(c => c.Name).FirstOrDefault(),
                    Address = a.Customers.Select(c => c.Address).FirstOrDefault() ?? "",
                    a.CreatedAt
                })
                .ToListAsync();

            var result = new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                Data = items
            };

            return ApiResponse<object>.Success(result);
        }

        public async Task<ApiResponse<object>> GetUserDetailAsync(int id)
        {
            var account = await _repo.GetDetailByIdAsync(id);
            if (account == null) return ApiResponse<object>.Fail("Không tìm thấy người dùng.", 404);

            var customer = account.Customers.FirstOrDefault();
            var totalOrders = customer?.Orders?.Count ?? 0;
            var totalSpent = customer?.Orders?.Sum(o => o.TotalCost) ?? 0;

            return ApiResponse<object>.Success(new
            {
                account.AccountId,
                UserId = customer?.UserId,
                account.Username,
                account.Email,
                Name = customer?.Name ?? "",
                Address = customer?.Address ?? "",
                TotalOrders = totalOrders,
                TotalSpent = totalSpent,
                account.CreatedAt
            });
        }

        public async Task<ApiResponse<object>> ResetPasswordAsync(int id)
        {
            var account = await _repo.GetBasicByIdAsync(id);
            if (account == null) return ApiResponse<object>.Fail("Không tìm thấy người dùng.", 404);

            // Logic hash mật khẩu đồng bộ với AuthRepository/Service
            const string DEFAULT_PASSWORD = "123456";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(DEFAULT_PASSWORD));
            account.Password = Convert.ToHexString(bytes).ToLower();

            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(null, $"Đã reset mật khẩu người dùng '{account.Username}' về mặc định: {DEFAULT_PASSWORD}")
                : ApiResponse<object>.Fail("Không có thay đổi nào được thực hiện hoặc lỗi hệ thống.", 500);
        }
    }
}