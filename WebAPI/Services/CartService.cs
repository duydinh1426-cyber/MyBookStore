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

        public async Task<ApiResponse<CartResponseDto>> GetCartAsync(int userId)
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

            var response = new CartResponseDto(
                itemDtos,
                itemDtos.Sum(i => i.SubTotal),
                itemDtos.Sum(i => i.Quantity)
            );

            return ApiResponse<CartResponseDto>.Success(response);
        }

        public async Task<ApiResponse<object>> AddToCartAsync(int userId, AddCartDto dto)
        {
            if (dto.Quantity <= 0)
                return ApiResponse<object>.Fail("Số lượng thêm vào phải lớn hơn 0.");

            var book = await _bookRepo.GetByIdAsync(dto.BookId);
            if (book == null)
                return ApiResponse<object>.Fail("Sách không tồn tại.", 404);

            if (book.NumberStock <= 0)
                return ApiResponse<object>.Fail("Sách hiện đã hết hàng.");

            var cartItem = await _repo.GetCartItemAsync(userId, dto.BookId);
            var targetQty = (cartItem?.Quantity ?? 0) + dto.Quantity;

            if (targetQty > book.NumberStock)
                return ApiResponse<object>.Fail($"Sách trong kho không đủ (Hiện có: {book.NumberStock}).");

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

            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(null, "Đã thêm vào giỏ hàng.")
                : ApiResponse<object>.Fail("Lỗi hệ thống khi lưu giỏ hàng.", 500);
        }

        public async Task<ApiResponse<object>> UpdateCartAsync(int userId, int bookId, UpdateCartDto dto)
        {
            var cartItem = await _repo.GetCartItemAsync(userId, bookId);
            if (cartItem == null)
                return ApiResponse<object>.Fail("Không tìm thấy sản phẩm trong giỏ hàng.", 404);

            if (dto.Quantity <= 0)
            {
                _repo.Delete(cartItem);
            }
            else
            {
                if (dto.Quantity > cartItem.Book.NumberStock)
                    return ApiResponse<object>.Fail($"Chỉ còn {cartItem.Book.NumberStock} sản phẩm trong kho.");

                cartItem.Quantity = dto.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                _repo.Update(cartItem);
            }

            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(null, "Cập nhật giỏ hàng thành công.")
                : ApiResponse<object>.Fail("Lỗi khi cập nhật giỏ hàng.", 500);
        }

        public async Task<ApiResponse<object>> RemoveFromCartAsync(int userId, int bookId)
        {
            var cartItem = await _repo.GetCartItemAsync(userId, bookId);
            if (cartItem == null)
                return ApiResponse<object>.Fail("Sản phẩm không tồn tại trong giỏ hàng.", 404);

            _repo.Delete(cartItem);
            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(null, "Đã xóa sản phẩm khỏi giỏ hàng.")
                : ApiResponse<object>.Fail("Lỗi khi xóa sản phẩm.", 500);
        }

        public async Task<ApiResponse<object>> ClearCartAsync(int userId)
        {
            var result = await _repo.ClearCartByUserIdAsync(userId);
            return result
                ? ApiResponse<object>.Success(null, "Giỏ hàng đã được làm trống.")
                : ApiResponse<object>.Fail("Lỗi khi xóa giỏ hàng.", 500);
        }
    }
}