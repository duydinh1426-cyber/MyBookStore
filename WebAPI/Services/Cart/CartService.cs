using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;
using WebAPI.Services.Helper;

namespace WebAPI.Services.Cart
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

        public async Task<ServiceResult<CartResponseDto>> GetCartAsync(int userId)
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

            var data = new CartResponseDto(
                itemDtos,
                itemDtos.Sum(i => i.SubTotal),
                itemDtos.Sum(i => i.Quantity)
            );

            return ServiceResult<CartResponseDto>.Success(data);
        }

        public async Task<ServiceResult> AddToCartAsync(int userId, AddCartDto dto)
        {
            if (dto.Quantity <= 0)
                return ServiceResult.Failure("Số lượng phải lớn hơn 0.");

            var book = await _bookRepo.GetByIdAsync(dto.BookId);
            if (book == null)
                return ServiceResult.Failure("Sách không tồn tại.");
            
            if (book.NumberStock <= 0)
                return ServiceResult.Failure("Sách hiện đã hết hàng.");

            var cartItem = await _repo.GetCartItemAsync(userId, dto.BookId);
            var targetQty = (cartItem?.Quantity ?? 0) + dto.Quantity;

            if (targetQty > book.NumberStock)
                return ServiceResult.Failure($"Chỉ còn {book.NumberStock} cuốn trong kho.");

            if (cartItem == null)
            {
                _repo.Add(new CartItem
                {
                    UserId = userId,
                    BookId = dto.BookId,
                    Quantity = dto.Quantity,
                    CreatedAt = TimeHelper.NowVietnam(),
                    UpdatedAt = TimeHelper.NowVietnam(),
                });
            }
            else
            {
                cartItem.Quantity = targetQty;
                cartItem.UpdatedAt = TimeHelper.NowVietnam();
                _repo.Update(cartItem);
            }

            var success = await _repo.SaveChangesAsync();
            if (!success)
                return ServiceResult.Failure("Lỗi hệ thống.", 500);

            return ServiceResult.Success("Đã thêm vào giỏ hàng");
        }

        public async Task<ServiceResult> UpdateCartAsync(int userId, int bookId, UpdateCartDto dto)
        {
            var cartItem = await _repo.GetCartItemAsync(userId, bookId);
            if (cartItem == null)
                return ServiceResult.Failure("Sách không có trong giỏ hàng.");

            if (dto.Quantity <= 0)
            {
                _repo.Delete(cartItem);
                return ServiceResult.Success("Đã xóa sách khỏi giỏ hàng.");
            }

            else
            {
                if (dto.Quantity > cartItem.Book.NumberStock)
                    return ServiceResult.Failure($"Chỉ còn {cartItem.Book.NumberStock} cuốn trong kho.");

                cartItem.Quantity = dto.Quantity;
                cartItem.UpdatedAt = TimeHelper.NowVietnam();
                _repo.Update(cartItem);
            }

            var success = await _repo.SaveChangesAsync();
            if (!success)
                return ServiceResult.Failure("Lỗi hệ thống khi cập nhật giỏ hàng.");

            return ServiceResult.Success("Cập nhật giỏ hàng thành công.");
        }

        public async Task<ServiceResult> RemoveFromCartAsync(int userId, int bookId)
        {
            var cartItem = await _repo.GetCartItemAsync(userId, bookId);
            if (cartItem == null)
                return ServiceResult.Failure("Sách không tồn tại trong giỏ hàng.");

            _repo.Delete(cartItem);
            var success = await _repo.SaveChangesAsync();

            if (!success)
                return ServiceResult.Failure("Lỗi hệ thống khi xóa sách trong giỏ hàng.", 500);

            return ServiceResult.Success("Đã xóa sách khỏi giỏ hàng.");
        }

        public async Task<ServiceResult> ClearCartAsync(int userId)
        {
            var result = await _repo.ClearCartByUserIdAsync(userId);
            if (!result)
                return ServiceResult.Failure("Lỗi khi xóa giỏ hàng.");

            return ServiceResult.Success("Đã xóa toàn bộ giỏ hàng.");

        }
    }
}