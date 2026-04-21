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

        public async Task<object> GetUserOrdersAsync(int userId, int page, int pageSize, string? status)
        {
            var query = _db.Orders
                .Where(o => o.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new {
                    orderId = o.OrderId,
                    totalCost = o.TotalCost,
                    status = o.Status,
                    statusLabel = o.Status == "pending" ? "Chờ xác nhận" :
                                  o.Status == "confirmed" ? "Đã xác nhận" :
                                  o.Status == "shipping" ? "Đang giao" :
                                  o.Status == "completed" ? "Hoàn thành" :
                                  o.Status == "cancelled" ? "Đã hủy" : "Không xác định",
                    phone = o.Phone,
                    address = o.Address,
                    note = o.Note,
                    createdAt = o.CreatedAt,
                    itemCount = o.OrderItems != null ? o.OrderItems.Count : 0,
                    paymentMethod = o.PaymentMethod,
                    isPaid = o.IsPaid,
                })
                .ToListAsync();

            return new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                data = items
            };
        }

        public async Task<object> GetAllOrdersAdminAsync(string? status, string? keyword, int page, int pageSize)
        {
            var query = _db.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(o =>
                    o.OrderId.ToString() == keyword ||
                    (o.Phone ?? "").Contains(keyword) ||
                    (o.User.Name ?? "").Contains(keyword));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OrderId,
                    customerName = o.User != null ? o.User.Name : "",
                    o.TotalCost,
                    o.Status,
                    o.CreatedAt,
                    o.Phone,
                    o.PaymentMethod,
                    o.IsPaid,
                    o.Address
                })
                .ToListAsync();

            return new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                data
            };
        }

        public async Task<List<CartItem>> GetCartItemsAsync(int userId)
        {
            return await _db.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<object> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // Base query với filter ngày
            var baseQuery = _db.Orders.AsQueryable();
            if (from.HasValue) baseQuery = baseQuery.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) baseQuery = baseQuery.Where(o => o.CreatedAt <= to.Value);

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
                .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalCost) })
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

        public void RemoveCartItems(IEnumerable<CartItem> items) => _db.CartItems.RemoveRange(items);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;

		public void AddRefundRequest(RefundRequest r) => _db.RefundRequests.Add(r);

		public async Task<List<RefundRequest>> GetRefundRequestsAsync(string? status)
		{
			var q = _db.RefundRequests
				.Include(r => r.Order)
				.Include(r => r.User)
				.AsQueryable();

			if (!string.IsNullOrEmpty(status))
				q = q.Where(r => r.Status == status);

			return await q.OrderByDescending(r => r.CreatedAt).ToListAsync();
		}

		public async Task<RefundRequest?> GetRefundRequestByIdAsync(int id)
		{
			return await _db.RefundRequests
				.Include(r => r.Order).ThenInclude(o => o.User)
				.Include(r => r.User)
				.FirstOrDefaultAsync(r => r.RefundRequestId == id);
		}
	}
}