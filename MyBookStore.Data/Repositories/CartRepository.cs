using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly DBContext _db;

        public CartRepository(DBContext db)
        {
            _db = db;
        }

        public async Task<List<CartItem>> GetCart(int userId)
        {
            return await _db.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<CartItem?> GetCartItem(int userId, int bookId)
        {
            return await _db.CartItems
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId);
        }

        public async Task AddAsync(CartItem item)
        {
            _db.CartItems.Add(item);
            await _db.SaveChangesAsync();
        }

        public async Task ClearCartAsync(int userId)
        {
            await _db.CartItems
                .Where(c => c.UserId == userId)
                .ExecuteDeleteAsync();
        }

        public async Task DeleteAsync(CartItem item)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(CartItem item)
        {
            _db.CartItems.Update(item);
            await _db.SaveChangesAsync();
        }
        
    }
}
