using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderByIdAsync(int id);
        Task<object> GetUserOrdersAsync(int userId, int page, int pageSize, string? status);
        Task<object> GetAllOrdersAdminAsync(string? status, string? keyword, int page, int pageSize);
        Task<object> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null);
        Task<List<CartItem>> GetCartItemsAsync(int userId);
        void AddOrder(Order order);
        void RemoveCartItems(IEnumerable<CartItem> items);
        Task<bool> SaveChangesAsync();
        void AddRefundRequest(RefundRequest r);
        Task<List<RefundRequest>> GetRefundRequestsAsync(string? status);
        Task<RefundRequest?> GetRefundRequestByIdAsync(int id);
    }
}