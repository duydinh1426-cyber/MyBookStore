using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<List<CartItem>> GetCart(int userId);
        Task<CartItem?> GetCartItem(int userId, int bookId);
        Task AddAsync(CartItem item);
        Task UpdateAsync(CartItem item);
        Task DeleteAsync(CartItem item);
        Task ClearCartAsync(int userId);
        Task SaveChangesAsync();
    }
}
