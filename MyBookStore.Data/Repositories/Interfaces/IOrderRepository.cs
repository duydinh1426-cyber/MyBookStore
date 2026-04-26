using Azure;
using MyBookStore.Data.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Data.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderByIdAsync(int id); // lấy đơn hàng theo id

        // lấy đơn hàng của user theo trạng thái + phân trang
        Task<(int total, int page, int pageSize, int totalPages, List<Order> data)> GetUserOrdersAsync(int userId, int page, int pageSize, string? status);

        // lấy toàn bộ đơn hàng cho admin + phân trang
        Task<(int total, int page, int pageSize, int totalPages, List<Order> data)> GetAllOrdersAdminAsync(string? status, string? keyword, int page, int pageSize);

        // kiểm tra user đã mua sách này chưa
        Task<bool> HasPurchasedAsync(int userId, int bookId);

        // thống kê: lấy đơn hàng trong khoảng thời gian
        Task<object> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null);

        void AddOrder(Order order);
        Task<bool> SaveChangesAsync();
    }
}