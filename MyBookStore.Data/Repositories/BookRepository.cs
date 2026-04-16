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

        public BookRepository(DBContext db)
        {
            _db = db;
        }

        public IQueryable<Book> GetQuery()
        {
            return _db.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .AsQueryable();
        }

        public async Task<Book?> GetByIdAsync(int id)
        {
            return await _db.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }
        
        public async Task AddAsync(Book book)
        {
            _db.Books.Add(book);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Book book)
        {
            _db.Books.Update(book);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Book book)
        {
            _db.Books.Remove(book);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> CategoryExists(int categoryId)
        {
            return await _db.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task<bool> HasOrderItems(int bookId)
        {
            return await _db.OrderItems.AnyAsync(oi => oi.BookId == bookId);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
