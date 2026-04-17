using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        IQueryable<Review> GetQuery();
        Task<Review?> GetByIdAsync(int id);
        Task<Review?> GetUserReviewAsync(int userId, int bookId);
        Task<bool> HasPurchasedAsync(int userId, int bookId);
        Task<bool> BookExistsAsync(int bookId);
        Task<Dictionary<int, int>> GetRatingStatsAsync(int bookId);
        void Add(Review review);
        void Delete(Review review);
        Task UpdateBookRatingAsync(int bookId);
        Task<bool> SaveChangesAsync();
    }
}