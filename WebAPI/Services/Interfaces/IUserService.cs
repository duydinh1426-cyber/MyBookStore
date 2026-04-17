using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<object>> GetAllUsersAsync(string? keyword, int page, int pageSize);
        Task<ApiResponse<object>> GetUserDetailAsync(int id);
        Task<ApiResponse<object>> ResetPasswordAsync(int id);
    }
}