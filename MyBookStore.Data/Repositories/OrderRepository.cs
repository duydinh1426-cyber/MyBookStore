using Data.Models;
using Data.QueryResult;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;

namespace Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly DBContext _db;
        public OrderRepository(DBContext db) => _db = db;

        // ─── Expose DbContext (dùng trong OrderService để tạo transaction) ────

        public DBContext GetDbContext() => _db;

        // ─── Order ───────────────────────────────────────────────────────────

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _db.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Book)
                .Include(o => o.User)
                    .ThenInclude(u => u.Account)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<(List<Order> Items, int Total)> GetUserOrdersAsync(
            int userId, int page, int pageSize, string? status)
        {
            var query = _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<Order> Items, int Total)> GetAllOrdersAdminAsync(
            string? status, string? keyword, int page, int pageSize)
        {
            var query = _db.Orders
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(o =>
                    o.OrderId.ToString() == keyword ||
                    (o.Phone ?? "").Contains(keyword) ||
                    (o.User.Name ?? "").Contains(keyword));

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public void AddOrder(Order order) => _db.Orders.Add(order);

        // ─── Cart ────────────────────────────────────────────────────────────

        public async Task<List<CartItem>> GetCartItemsAsync(int userId)
        {
            return await _db.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Load cart items kèm lock UPDLOCK trên bảng Books để tránh 2 user
        /// checkout cùng 1 cuốn sách cùng lúc (race condition).
        /// Phải được gọi bên trong một transaction đang mở.
        /// </summary>
        public async Task<List<CartItem>> GetCartItemsWithLockAsync(int userId)
        {
            // Lấy BookIds trong giỏ hàng trước
            var bookIds = await _db.CartItems
                .Where(c => c.UserId == userId)
                .Select(c => c.BookId)
                .ToListAsync();

            if (!bookIds.Any())
                return new List<CartItem>();

            // Lock các book rows với UPDLOCK — ngăn transaction khác đọc/ghi
            // trong khi ta đang kiểm tra và trừ tồn kho
            var inClause = string.Join(",", bookIds);
            await _db.Database.ExecuteSqlRawAsync(
                $"SELECT BookId FROM Books WITH (UPDLOCK) WHERE BookId IN ({inClause})");

            // Bây giờ load đầy đủ cart items với book đã bị lock
            return await _db.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public void RemoveCartItems(IEnumerable<CartItem> items) =>
            _db.CartItems.RemoveRange(items);

        // ─── Stats ───────────────────────────────────────────────────────────

        public async Task<decimal> GetRevenueAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Orders.Where(o => o.Status == "completed");
            if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
            return await query.SumAsync(o => (decimal?)o.TotalCost) ?? 0;
        }

        public async Task<decimal> GetMonthlyRevenueAsync()
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            return await _db.Orders
                .Where(o => o.Status == "completed" && o.CreatedAt >= startOfMonth)
                .SumAsync(o => (decimal?)o.TotalCost) ?? 0;
        }

        public async Task<int> GetTotalOrdersAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Orders.AsQueryable();
            if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
            return await query.CountAsync();
        }

        public async Task<int> GetTotalBooksSoldAsync(DateTime? from, DateTime? to)
        {
            return await _db.OrderItems
                .Where(oi =>
                    oi.Order.Status == "completed" &&
                    (!from.HasValue || oi.Order.CreatedAt >= from.Value) &&
                    (!to.HasValue || oi.Order.CreatedAt <= to.Value))
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;
        }

        public async Task<List<OrderStatusStat>> GetStatusStatsAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Orders.AsQueryable();
            if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);

            return await query
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusStat
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(o => o.TotalCost)
                })
                .ToListAsync();
        }

        // ─── Refund ──────────────────────────────────────────────────────────

        public void AddRefundRequest(RefundRequest r) => _db.RefundRequests.Add(r);

        public async Task<List<RefundRequest>> GetRefundRequestsAsync(string? status)
        {
            var query = _db.RefundRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                    .ThenInclude(u => u.Account)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public async Task<RefundRequest?> GetRefundRequestByIdAsync(int id)
        {
            return await _db.RefundRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.User)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RefundRequestId == id);
        }

        // ─── Persistence ─────────────────────────────────────────────────────

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}