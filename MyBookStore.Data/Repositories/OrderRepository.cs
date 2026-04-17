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
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Book)
                .Include(o => o.User)
                    .ThenInclude(u => u.Account)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            return await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersAdminAsync(string? status, string? keyword)
        {
            var query = _db.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(o => o.OrderId.ToString() == keyword ||
                                         (o.Phone ?? "").Contains(keyword) ||
                                         (o.User.Name ?? "").Contains(keyword));
            }

            return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public async Task<List<CartItem>> GetCartItemsAsync(int userId)
        {
            return await _db.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<object> GetAdminStatsAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // Tổng doanh thu (chỉ tính các đơn không bị hủy)
            var totalRevenue = await _db.Orders
                .Where(o => o.Status != "CANCELLED")
                .SumAsync(o => o.TotalCost);

            // Doanh thu tháng này
            var monthlyRevenue = await _db.Orders
                .Where(o => o.Status != "CANCELLED" && o.CreatedAt >= startOfMonth)
                .SumAsync(o => o.TotalCost);

            // Thống kê theo trạng thái
            var statusCounts = await _db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Tổng số sách đã bán
            var totalBooksSold = await _db.OrderItems
                .Where(oi => oi.Order.Status != "CANCELLED")
                .SumAsync(oi => oi.Quantity);

            return new
            {
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                TotalOrders = await _db.Orders.CountAsync(),
                TotalBooksSold = totalBooksSold,
                StatusDistribution = statusCounts
            };
        }

        public void AddOrder(Order order) => _db.Orders.Add(order);

        public void RemoveCartItems(IEnumerable<CartItem> items) => _db.CartItems.RemoveRange(items);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}