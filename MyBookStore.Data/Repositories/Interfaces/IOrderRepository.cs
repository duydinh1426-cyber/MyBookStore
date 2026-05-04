<<<<<<< HEAD
﻿using MyBookStore.Data.Models;
=======
﻿using Data.QueryResult;
using MyBookStore.Data.Models;
>>>>>>> f84b213 ( new chatbox)

namespace Data.Repositories.Interfaces
{
    public interface IOrderRepository
    {
<<<<<<< HEAD
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
=======
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
>>>>>>> f84b213 ( new chatbox)
    }
}