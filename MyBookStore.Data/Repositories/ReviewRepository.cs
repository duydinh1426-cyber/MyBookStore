using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;

namespace Data.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly DBContext _db;
        public ReviewRepository(DBContext db) => _db = db;

        public async Task<(int total, int page, int pageSize, int totalPages, List<Review> data)> GetReviewAsync
            (int bookId, int page, int pageSize, int? rating)
        {
            var total = await _db.Reviews
                .Where(r => r.BookId == bookId &&
                    (!rating.HasValue || r.Rating == rating.Value))
                .CountAsync();

            var data = await _db.Reviews
                .Where(r => r.BookId == bookId && 
                    (!rating.HasValue || r.Rating == rating.Value))
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.User)
                .Include(r => r.Book)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return (totalPages, page, pageSize, totalPages, data);
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _db.Reviews.FindAsync(id);
        }

        public async Task<Review?> GetUserReviewAsync(int userId, int bookId)
        {
            return await _db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);
        }

        public async Task<Dictionary<int, int>> GetRatingStatsAsync(int bookId)
        {
            return await _db.Reviews
                .Where(r => r.BookId == bookId)
                .GroupBy(r => r.Rating)
                .Select(g => new 
                    { 
                        Rating = g.Key, 
                        Count = g.Count() 
                    })
                .ToDictionaryAsync(x => x.Rating, x => x.Count);
        }

        public void Add(Review review) => _db.Reviews.Add(review);
        public void Delete(Review review) => _db.Reviews.Remove(review);

        public async Task UpdateBookRatingAsync(int bookId)
        {
            var book = await _db.Books.FindAsync(bookId);
            if (book == null) return;

            var stats = await _db.Reviews
                .Where(r => r.BookId == bookId)
                .GroupBy(r => r.BookId)
                .Select(g => new {
                    Avg = g.Average(r => (double)r.Rating),
                    Count = g.Count()
                })
                .FirstOrDefaultAsync();

            book.AvgRating = stats != null ? (decimal)Math.Round(stats.Avg, 2) : 0;
            book.ReviewCount = stats?.Count ?? 0;
        }

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}