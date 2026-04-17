using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;

namespace Data.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly DBContext _db;
        public ReviewRepository(DBContext db) => _db = db;

        public IQueryable<Review> GetQuery() =>
            _db.Reviews.Include(r => r.User).Include(r => r.Book).AsQueryable();

        public async Task<Review?> GetByIdAsync(int id) =>
            await _db.Reviews.FindAsync(id);

        public async Task<Review?> GetUserReviewAsync(int userId, int bookId) =>
            await _db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);

        public async Task<bool> HasPurchasedAsync(int userId, int bookId) =>
            await _db.OrderItems.AnyAsync(oi => oi.Order.UserId == userId && oi.BookId == bookId);

        public async Task<bool> BookExistsAsync(int bookId) =>
            await _db.Books.AnyAsync(b => b.BookId == bookId);

        public async Task<Dictionary<int, int>> GetRatingStatsAsync(int bookId)
        {
            return await _db.Reviews
                .Where(r => r.BookId == bookId)
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Rating, x => x.Count);
        }

        public void Add(Review review) => _db.Reviews.Add(review);
        public void Delete(Review review) => _db.Reviews.Remove(review);

        public async Task UpdateBookRatingAsync(int bookId)
        {
            var book = await _db.Books.FindAsync(bookId);
            if (book != null)
            {
                var reviews = _db.Reviews.Where(r => r.BookId == bookId);
                book.ReviewCount = await reviews.CountAsync();
                book.AvgRating = book.ReviewCount > 0
                    ? (decimal)await reviews.AverageAsync(r => r.Rating)
                    : 0;
            }
        }

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}