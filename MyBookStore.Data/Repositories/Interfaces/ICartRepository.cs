using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<List<CartItem>> GetCartByUserIdAsync(int userId);
        Task<CartItem?> GetCartItemAsync(int userId, int bookId);
        void Add(CartItem item);
        void Update(CartItem item);
        void Delete(CartItem item);
        Task<bool> ClearCartByUserIdAsync(int userId);
        Task<bool> SaveChangesAsync();
    }
}
