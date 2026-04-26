using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;

namespace Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly DBContext _db;
        public OrderRepository(DBContext db) => _db = db;

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .Include(o => o.User)
                .ThenInclude(u => u.Account)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<(int total, int page, int pageSize, int totalPages, List<Order> data)> GetUserOrdersAsync
            (int userId, int page, int pageSize, string? status)
        {
            var query = _db.Orders
                .Where(o => o.UserId == userId)
                .AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var total = await query.CountAsync();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            var data = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.OrderItems)
                .ToListAsync();

            return (total, page, pageSize, totalPages, data);
        }

        public async Task<(int total, int page, int pageSize, int totalPages, List<Order> data)> GetAllOrdersAdminAsync
            (string? status, string? keyword, int page, int pageSize)
        {
            var query = _db.Orders
                .Include(o => o.User)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(o =>
                    o.OrderId.ToString() == keyword ||
                    (o.Phone ?? "").Contains(keyword) ||
                    (o.User.Name ?? "").Contains(keyword));

            var total = await query.CountAsync();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            var data = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (total, page, pageSize, totalPages, data);
        }

        public async Task<bool> HasPurchasedAsync(int userId, int bookId)
        {
            return await _db.OrderItems.AnyAsync(oi =>
                oi.Order.UserId == userId &&
                oi.BookId == bookId &&
                oi.Order.Status == "completed");
        }

        public async Task<object> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            TimeZoneInfo vn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vn); // set giờ Việt Nam
            var startOfMonth = new DateTime(now.Year, now.Month, 1); // ngày đầu trong tháng

            // lọc ngày
            var baseQuery = _db.Orders.AsNoTracking();
            if (from.HasValue) 
                baseQuery = baseQuery.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) 
                baseQuery = baseQuery.Where(o => o.CreatedAt <= to.Value);

            // Tổng doanh thu - chỉ tính đơn HOÀN THÀNH trong khoảng ngày được chọn
            var totalRevenue = await baseQuery
                .Where(o => o.Status == "completed")
                .SumAsync(o => o.TotalCost);

            // Doanh thu tháng này - luôn tính theo tháng hiện tại, không bị ảnh hưởng filter
            var monthlyRevenue = await _db.Orders
                .Where(o => o.Status == "completed" && o.CreatedAt >= startOfMonth)
                .SumAsync(o => o.TotalCost);

            // Thống kê theo trạng thái - dùng baseQuery để filter ngày
            var statusCounts = await baseQuery
                .GroupBy(o => o.Status)
                .Select(g => new // nhóm theo trạng thái rồi thống kê mỗi nhóm
                { 
                    Status = g.Key, 
                    Count = g.Count(), 
                    Revenue = g.Sum(o => o.TotalCost)
                })
                .ToListAsync();

            // Tổng số sách đã bán - dùng baseQuery để filter ngày
            var totalBooksSold = await _db.OrderItems
                .Where(oi => oi.Order.Status == "completed" &&
                             (!from.HasValue || oi.Order.CreatedAt >= from.Value) &&
                             (!to.HasValue || oi.Order.CreatedAt <= to.Value))
                .SumAsync(oi => oi.Quantity);

            return new
            {
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                TotalOrders = await baseQuery.CountAsync(),
                TotalBooksSold = totalBooksSold,
                StatusDistribution = statusCounts
            };
        }

        public void AddOrder(Order order) => _db.Orders.Add(order);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}