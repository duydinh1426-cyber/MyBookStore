using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface ICartService
    {
        Task<ApiResponse<CartResponseDto>> GetCart(int userId);
        Task<ApiResponse<object>> AddToCart(int userId, AddCartDto dto);
        Task<ApiResponse<object>> UpdateCart(int userId, int bookId, UpdateCartDto dto);
        Task<ApiResponse<object>> RemoveFromCart(int userId, int bookId);
        Task<ApiResponse<object>> ClearCart(int userId);
    }
}
