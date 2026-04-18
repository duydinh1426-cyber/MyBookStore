using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartResponseDto> GetCartAsync(int userId);
        Task<string?> AddToCartAsync(int userId, AddCartDto dto);
        Task<string?> UpdateCartAsync(int userId, int bookId, UpdateCartDto dto);
        Task<string?> RemoveFromCartAsync(int userId, int bookId);
        Task<string?> ClearCartAsync(int userId);
    }
}