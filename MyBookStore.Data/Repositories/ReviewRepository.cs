using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly DBContext _db;

        public ReviewRepository(DBContext db)
        {
            _db = db;
        }

        public async Task AddReview(Review review)
        {
            _db.Reviews.Add(review);
        }

        public async Task<bool> AlreadyReviewed(int userId, int bookId)
        {
            return await _db.Reviews.AnyAsync(r => r.BookId == bookId && r.UserId == userId);
        }

        public async Task<bool> BookExists(int bookId)
        {
            return await _db.Books.AnyAsync(b => b.BookId == bookId);
        }

        public async Task<int> CountReviews(IQueryable<Review> query)
        {
            return await query.CountAsync();
        }

        public async Task DeleteReview(Review review)
        {
            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
        }

        public IQueryable<Review> GetAll()
        {
            return _db.Reviews
                .Include(r => r.User)
                .Include(r => r.Book);
        }

        public async Task<double> GetAverageRating(IQueryable<Review> query)
        {
            return await query.AnyAsync()
                ? await query.AverageAsync(r => (double)r.Rating) : 0.0 ;
        }

        public Task<Review?> GetById(int id)
        {
            return _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
        }

        public async Task<List<Review>> GetPagedReviews(IQueryable<Review> query, int page, int pageSize)
        {
            return await query
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetRatingStats(int bookId)
        {
            return await _db.Reviews
            .Where(r => r.BookId == bookId)
            .GroupBy(r => r.Rating)
            .Select(g => new { star = g.Key, count = g.Count() })
            .ToDictionaryAsync(x => x.star, x => x.count);
        }

        public IQueryable<Review> GetReviewsByBook(int bookId)
        {
            return _db.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.BookId == bookId);
        }

        public async Task<Review?> GetUserReview(int userId, int bookId)
        {
            return await _db.Reviews
            .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId);
        }

        public async Task<bool> HasPurchased(int userId, int bookId)
        {
            return await _db.OrderItems.AnyAsync(oi =>
            oi.BookId == bookId &&
            oi.Order.UserId == userId &&
            oi.Order.Status == "completed");
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task UpdateBookRating(int bookId)
        {
            var book = await _db.Books.FindAsync(bookId);
            if (book == null) return;

            var stats = await _db.Reviews
                .Where(r => r.BookId == bookId)
                .GroupBy(r => r.BookId)
                .Select(g => new
                {
                    avg = g.Average(r => (double)r.Rating),
                    count = g.Count()
                })
                .FirstOrDefaultAsync();

            book.AvgRating = stats != null ? (decimal)Math.Round(stats.avg, 2) : 0;
            book.ReviewCount = stats?.count ?? 0;
        }
    }
}
