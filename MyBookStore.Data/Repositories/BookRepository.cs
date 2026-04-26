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

        public async Task<(int total, int page, int pageSize, int totalPages, List<Book> data)> GetBookAsync(
            int page = 1,
            int pageSize = 10,
            string? keyword = null,
            int? categoryId = null,
            decimal? minPrice = null, 
            decimal? maxPrice = null, 
            string sortBy = "createdAt", 
            string sortOrder = "desc")
        {
            var query = _db.Books
                .Include(b => b.Category)
                .AsNoTracking();

            // filter
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(b => 
                    b.Title.Contains(keyword) ||
                    (b.Author != null && b.Author.ToLower().Contains(keyword))); // check author khác null và lọc
            }

            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId);

            if (minPrice.HasValue)
                query = query.Where(b => b.Price >= minPrice);

            if (maxPrice.HasValue)
                query = query.Where(b => b.Price <= maxPrice);

            // sort
            query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
            {
                ("price", "asc") => query.OrderBy(b => b.Price), // sort theo giá 
                ("price", _) => query.OrderByDescending(b => b.Price),
                ("title", "asc") => query.OrderBy(b => b.Title), //sort theo tên
                ("title", _) => query.OrderByDescending(b => b.Title),
                ("numbersold", "asc") => query.OrderBy(b => b.NumberSold), // sort theo số lượng bán
                ("numbersold", _) => query.OrderByDescending(b => b.NumberSold),
                ("avgrating", "asc") => query.OrderBy(b => b.AvgRating), // sort theo rating 
                ("avgrating", _) => query.OrderByDescending(b => b.AvgRating),
                (_, "asc") => query.OrderBy(b => b.CreatedAt), // default
                _ => query.OrderByDescending(b => b.CreatedAt)
            };

            var total = await query.CountAsync();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (total, page, pageSize, totalPages, data); ;
        }

        public async Task<List<Book>> GetTopNewAsync(int count)
        {
            return await _db.Books
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Book>> GetTopSellingAsync(int count)
        {
            return await _db.Books
                .AsNoTracking()
                .OrderByDescending(b => b.NumberStock)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Book>> GetTopRatedAsync(int count)
        {
            return await _db.Books
                .AsNoTracking()
                .Where(b => b.ReviewCount > 0)
                .OrderByDescending(b => b.AvgRating)
                .ThenByDescending(b => b.ReviewCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Book?> GetByIdAsync(int id)
        {
            return await _db.Books
                .Include(c => c.Category)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<bool> HasOrderItemsAsync(int bookId)
        {
            return await _db.OrderItems.AnyAsync(b => b.BookId == bookId);
        }

        public void Add(Book book) => _db.Books.Add(book);

        public void Update(Book book) => _db.Books.Update(book);

        public void Delete(Book book) => _db.Books.Remove(book);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}
