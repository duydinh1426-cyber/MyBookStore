using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<List<CartItem>> GetCartItems(int userId);
        Task<Order?> GetOrderById(int id);  
        Task AddOrder(Order item);
        void RemoveCartItems(List<CartItem> items);
        Task SaveChangesAsync();
    }
}
