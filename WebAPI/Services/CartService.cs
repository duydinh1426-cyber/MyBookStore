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

        public async Task<ApiResponse<CartResponseDto>> GetCart(int userId)
        {
            var items = await _repo.GetCart(userId);

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

            var response = new CartResponseDto(
                itemDtos,
                itemDtos.Sum(i => i.SubTotal),
                itemDtos.Sum(i => i.Quantity)
            );

            return ApiResponse<CartResponseDto>.Success(response);
        }

        public async Task<ApiResponse<object>> AddToCart(int userId, AddCartDto dto)
        {
            if (dto.Quantity <= 0)
                return ApiResponse<object>.Fail("Số lượng phải lớn hơn 0.");

            var book = await _bookRepo.GetByIdAsync(dto.BookId);
            if (book == null)
                return ApiResponse<object>.Fail("Sách không tồn tại.", 404);

            if (book.NumberStock <= 0)
                return ApiResponse<object>.Fail("Sách đã hết hàng.");

            var cartItem = await _repo.GetCartItem(userId, dto.BookId);
            var currentQty = cartItem?.Quantity ?? 0;

            if (currentQty + dto.Quantity > book.NumberStock)
                return ApiResponse<object>.Fail($"Chỉ còn {book.NumberStock} cuốn trong kho.");

            if (cartItem == null)
            {
                await _repo.AddAsync(new CartItem
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
                cartItem.Quantity += dto.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(cartItem);
            }

            return ApiResponse<Object>.Success(null, "Đã thêm vào giỏ hàng.");
        }

        public async Task<ApiResponse<Object>> ClearCart(int userId)
        {
            await _repo.ClearCartAsync(userId);
            return ApiResponse<Object>.Success(null, "Đã xóa toàn bộ giỏ hàng.");
        }

        public async Task<ApiResponse<Object>> RemoveFromCart(int userId, int bookId)
        {
            var cartItem = await _repo.GetCartItem(userId, bookId);
            if (cartItem == null)
                return ApiResponse<Object>.Fail("Sách không có trong giỏ hàng.", 404);

            await _repo.DeleteAsync(cartItem);

            return ApiResponse<Object>.Success(null, "Đã xóa sách khỏi giỏ hàng.");
        }

        public async Task<ApiResponse<Object>> UpdateCart(int userId, int bookId, UpdateCartDto dto)
        {
            var cartItem = await _repo.GetCartItem(userId, bookId);
            if (cartItem == null)
                return ApiResponse<Object>.Fail("Sách không có trong giỏ hàng.", 404);

            if (dto.Quantity <= 0)
            {
                await _repo.DeleteAsync(cartItem);
                return ApiResponse<Object>.Success(null, "Đã xóa sách khỏi giỏ hàng.");
            }

            if (dto.Quantity > cartItem.Book.NumberStock)
                return ApiResponse<Object>.Fail($"Chỉ còn {cartItem.Book.NumberStock} cuốn trong kho.");

            cartItem.Quantity = dto.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(cartItem);

            return ApiResponse<Object>.Success(null, "Đã cập nhật giỏ hàng.");
        }
    }
}
