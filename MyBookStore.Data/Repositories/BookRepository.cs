using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly DBContext _db;

        public BookRepository(DBContext db) => _db = db;

        public IQueryable<Book> GetQuery()
        {
            return _db.Books
                .Include(b => b.Category)
                .AsQueryable();
        }

        public async Task<Book?> GetByIdAsync(int id)
        {
            return await _db.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public void Add(Book book) => _db.Books.Add(book);

        public void Update(Book book) => _db.Books.Update(book);

        public void Delete(Book book) => _db.Books.Remove(book);

        public async Task<bool> CategoryExistsAsync(int categoryId)
        {
            return await _db.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task<bool> HasOrderItemsAsync(int bookId)
        {
            return await _db.OrderItems.AnyAsync(oi => oi.BookId == bookId);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
