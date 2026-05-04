using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;

namespace Data.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly DBContext _db;

        public CartRepository(DBContext db) => _db = db;

        public async Task<List<CartItem>> GetCartByUserIdAsync(int userId)
        {
            return await _db.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<CartItem?> GetCartItemAsync(int userId, int bookId)
        {
            return await _db.CartItems
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId);
        }

        public void Add(CartItem item) => _db.CartItems.Add(item);

        public void Update(CartItem item) => _db.CartItems.Update(item);

        public void Delete(CartItem item) => _db.CartItems.Remove(item);

        public async Task<bool> ClearCartByUserIdAsync(int userId)
        {
            // Sử dụng ExecuteDeleteAsync để tối ưu hiệu năng khi xóa hàng loạt
            var rows = await _db.CartItems
                .Where(c => c.UserId == userId)
                .ExecuteDeleteAsync();
            return rows >= 0;
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}