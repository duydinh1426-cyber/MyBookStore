using MyBookStore.Data.Models;
using WebAPI.DTOs;

namespace WebAPI.Services.Orders
{
    public interface IOrderService
    {
        Task<ServiceResult<OrderResponseDto>> CheckoutAsync(int userId, CheckoutDto dto);
        Task<ServiceResult<object>> GetByIdAsync(int userId, bool isAdmin, int id);
        Task<ServiceResult<object>> GetUserOrdersAsync(int userId, int page, int pageSize, string? status);
        Task<ServiceResult<object>> AdminGetAllOrdersAsync(string? status, string? keyword, int page, int pageSize);
        Task<ServiceResult<object>> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null);
        Task<ServiceResult<object>> CancelAsync(int userId, int id, CancelOrderDto? dto);
        Task<ServiceResult<object>> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<ServiceResult<object>> GetRefundRequestsAsync(string? status);
        Task<ServiceResult> ResolveRefundAsync(int refundId, string? adminNote);
        Task<ServiceResult<object>> GetOrderByIdAsync(int orderId, int userId);
    }
}