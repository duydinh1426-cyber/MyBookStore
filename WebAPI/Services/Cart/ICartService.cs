using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface ICartService
    {
        Task<ServiceResult<CartResponseDto>> GetCartAsync(int userId);
        Task<ServiceResult> AddToCartAsync(int userId, AddCartDto dto);
        Task<ServiceResult> UpdateCartAsync(int userId, int bookId, UpdateCartDto dto);
        Task<ServiceResult> RemoveFromCartAsync(int userId, int bookId);
        Task<ServiceResult> ClearCartAsync(int userId);
    }
}