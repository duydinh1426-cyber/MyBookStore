using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<List<CartItem>> GetCartByUserIdAsync(int userId); // lấy toàn bộ giỏ hàng của một user
        Task<CartItem?> GetCartItemAsync(int userId, int bookId); // lấy một item cụ thể trong giỏ
        Task<bool> ClearCartByUserIdAsync(int userId); // xóa toàn bộ giỏ hàng
        void Add(CartItem item);
        void Update(CartItem item);
        void Delete(CartItem item);
        Task<bool> SaveChangesAsync();
    }
}
