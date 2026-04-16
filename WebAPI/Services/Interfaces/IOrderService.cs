using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<object>> Checkout(int userId, CheckoutDto dto);
        Task<ApiResponse<object>> GetById(int userId, bool isAdmin, int id);
        Task<ApiResponse<object>> Cancel(int userId, int id);
        Task<ApiResponse<object>> UpdateStatus(int id, UpdateOrderStatusDto dto);
    }
}
