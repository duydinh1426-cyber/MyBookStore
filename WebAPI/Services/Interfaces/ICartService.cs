using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface ICartService
    {
        Task<ApiResponse<CartResponseDto>> GetCartAsync(int userId);
        Task<ApiResponse<object>> AddToCartAsync(int userId, AddCartDto dto);
        Task<ApiResponse<object>> UpdateCartAsync(int userId, int bookId, UpdateCartDto dto);
        Task<ApiResponse<object>> RemoveFromCartAsync(int userId, int bookId);
        Task<ApiResponse<object>> ClearCartAsync(int userId);
    }
}