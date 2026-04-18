using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderByIdAsync(int id);
        Task<object> GetUserOrdersAsync(int userId, int page, int pageSize, string? status);
        Task<List<Order>> GetAllOrdersAdminAsync(string? status, string? keyword);
        Task<object> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null);
        Task<List<CartItem>> GetCartItemsAsync(int userId);
        void AddOrder(Order order);
        void RemoveCartItems(IEnumerable<CartItem> items);
        Task<bool> SaveChangesAsync();
    }
}