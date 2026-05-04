using Data.QueryResult;
using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        // ─── Order ───────────────────────────────────────────────────────────
        Task<Order?> GetOrderByIdAsync(int id);
        Task<(List<Order> Items, int Total)> GetUserOrdersAsync(int userId, int page, int pageSize, string? status);
        Task<(List<Order> Items, int Total)> GetAllOrdersAdminAsync(string? status, string? keyword, int page, int pageSize);
        void AddOrder(Order order);

        // ─── Cart ────────────────────────────────────────────────────────────
        Task<List<CartItem>> GetCartItemsAsync(int userId);
        void RemoveCartItems(IEnumerable<CartItem> items);

        // ─── Stats ───────────────────────────────────────────────────────────
        Task<decimal> GetRevenueAsync(DateTime? from, DateTime? to);
        Task<decimal> GetMonthlyRevenueAsync();
        Task<int> GetTotalOrdersAsync(DateTime? from, DateTime? to);
        Task<int> GetTotalBooksSoldAsync(DateTime? from, DateTime? to);
        Task<List<OrderStatusStat>> GetStatusStatsAsync(DateTime? from, DateTime? to);

        // ─── Refund ──────────────────────────────────────────────────────────
        void AddRefundRequest(RefundRequest r);
        Task<List<RefundRequest>> GetRefundRequestsAsync(string? status);
        Task<RefundRequest?> GetRefundRequestByIdAsync(int id);

        // ─── Persistence ─────────────────────────────────────────────────────
        Task<bool> SaveChangesAsync();
    }
}