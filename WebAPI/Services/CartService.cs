using Data;
using Data.Repositories.Interfaces;
using MyBookStore.Data.Models;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _repo;
        private readonly IBookRepository _bookRepo;

        public CartService(ICartRepository repo, IBookRepository bookRepo)
        {
            _repo = repo;
            _bookRepo = bookRepo;
        }

        public async Task<CartResponseDto> GetCartAsync(int userId)
        {
            var items = await _repo.GetCartByUserIdAsync(userId);

            var itemDtos = items.Select(i => new CartItemResponseDto(
                i.CartItemId,
                i.BookId,
                i.Book.Title,
                i.Book.Author ?? "",
                i.Book.Image,
                i.Book.Price,
                i.Quantity,
                i.Book.Price * i.Quantity
            )).ToList();

            return new CartResponseDto(
                itemDtos,
                itemDtos.Sum(i => i.SubTotal),
                itemDtos.Sum(i => i.Quantity)
            );
        }

        public async Task<string?> AddToCartAsync(int userId, AddCartDto dto)
        {
            if (dto.Quantity <= 0)
            {
                return "Số lượng phải lớn hơn 0.";
            }

            var book = await _bookRepo.GetByIdAsync(dto.BookId);
            if (book == null)
            {
                return "Sách không tồn tại.";
            }

            if (book.NumberStock <= 0)
            {
                return "Sách hiện đã hết hàng.";
            }

            var cartItem = await _repo.GetCartItemAsync(userId, dto.BookId);
            var targetQty = (cartItem?.Quantity ?? 0) + dto.Quantity;

            if (targetQty > book.NumberStock)
            {
                return $"Chỉ còn {book.NumberStock} cuốn trong kho.";
            }

            if (cartItem == null)
            {
                _repo.Add(new CartItem
                {
                    UserId = userId,
                    BookId = dto.BookId,
                    Quantity = dto.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                cartItem.Quantity = targetQty;
                cartItem.UpdatedAt = DateTime.UtcNow;
                _repo.Update(cartItem);
            }

            var success = await _repo.SaveChangesAsync();
            return success ? null : "Lỗi hệ thống khi cập nhật giỏ hàng.";
        }

        public async Task<string?> UpdateCartAsync(int userId, int bookId, UpdateCartDto dto)
        {
            var cartItem = await _repo.GetCartItemAsync(userId, bookId);
            if (cartItem == null)
            {
                return "Sách không có trong giỏ hàng.";
            }

            if (dto.Quantity <= 0)
            {
                _repo.Delete(cartItem);
            }
            else
            {
                if (dto.Quantity > cartItem.Book.NumberStock)
                {
                    return $"Chỉ còn {cartItem.Book.NumberStock} cuốn trong kho.";
                }

                cartItem.Quantity = dto.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                _repo.Update(cartItem);
            }

            var success = await _repo.SaveChangesAsync();
            return success ? null : "Lỗi khi cập nhật giỏ hàng.";
        }

        public async Task<string?> RemoveFromCartAsync(int userId, int bookId)
        {
            var cartItem = await _repo.GetCartItemAsync(userId, bookId);
            if (cartItem == null)
            {
                return "Sách không tồn tại trong giỏ hàng.";
            }

            _repo.Delete(cartItem);
            var success = await _repo.SaveChangesAsync();
            return success ? null : "Lỗi khi xóa sản phẩm.";
        }

        public async Task<string?> ClearCartAsync(int userId)
        {
            var result = await _repo.ClearCartByUserIdAsync(userId);
            return result ? null : "Lỗi khi làm trống giỏ hàng.";
        }
    }
}