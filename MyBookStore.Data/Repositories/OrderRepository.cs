using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly DBContext _db;

        public OrderRepository(DBContext db)
        {
            _db = db;
        }

        public async Task AddOrder(Order item)
        {
            _db.Orders.Add(item);
            await _db.SaveChangesAsync();

        }

        public async Task<List<CartItem>> GetCartItems(int userId)
        {
            return await _db.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderById(int id)
        {
            return await _db.Orders
                .Include(o => o.OrderItems)!.ThenInclude(oi => oi.Book)
                .Include(o => o.User).ThenInclude(c => c.Account)
                .FirstOrDefaultAsync(o => o.UserId == id);
        }

        public void RemoveCartItems(List<CartItem> items)
        {
            _db.CartItems.RemoveRange(items);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
