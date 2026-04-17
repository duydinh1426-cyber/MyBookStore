using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<object>> CheckoutAsync(int userId, CheckoutDto dto);
        Task<ApiResponse<object>> GetByIdAsync(int userId, bool isAdmin, int id);
        Task<ApiResponse<object>> GetUserOrdersAsync(int userId);
        Task<ApiResponse<object>> AdminGetAllOrdersAsync(string? status, string? keyword);
        Task<ApiResponse<object>> CancelAsync(int userId, int id);
        Task<ApiResponse<object>> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<ApiResponse<object>> GetAdminStatsAsync();
    }
}