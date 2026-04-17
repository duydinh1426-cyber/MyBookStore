using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderByIdAsync(int id);
        Task<List<Order>> GetUserOrdersAsync(int userId);
        Task<List<Order>> GetAllOrdersAdminAsync(string? status, string? keyword);
        Task<object> GetAdminStatsAsync();
        Task<List<CartItem>> GetCartItemsAsync(int userId);
        void AddOrder(Order order);
        void RemoveCartItems(IEnumerable<CartItem> items);
        Task<bool> SaveChangesAsync();
    }
}