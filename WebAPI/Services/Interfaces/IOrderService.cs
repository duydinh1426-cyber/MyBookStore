using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IOrderService
    {
        Task<object> CheckoutAsync(int userId, CheckoutDto dto);
        Task<object?> GetByIdAsync(int userId, bool isAdmin, int id);
        Task<object> GetUserOrdersAsync(int userId, int page, int pageSize, string? status);
        Task<object> AdminGetAllOrdersAsync(string? status, string? keyword, int page, int pageSize);
        Task<object> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null);
        Task<object> CancelAsync(int userId, int id, CancelOrderDto? dto);
        Task<object> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<object> GetRefundRequestsAsync(string? status);
        Task<object> ResolveRefundAsync(int refundId, string? adminNote);
    }
}