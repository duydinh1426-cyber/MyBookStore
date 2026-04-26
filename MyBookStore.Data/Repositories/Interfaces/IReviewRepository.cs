using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        // lấy danh sách review + phân trang
        Task<(int total, int page, int pageSize, int totalPages, List<Review> data)> GetReviewAsync
            (int bookId, int page, int pageSize, int? rating);
        Task<Review?> GetByIdAsync(int id); // lấy review theo id

        // lấy review của 1 user cho 1 khách
        Task<Review?> GetUserReviewAsync(int userId, int bookId); 

        // thống kê review
        Task<Dictionary<int, int>> GetRatingStatsAsync(int bookId);
        void Add(Review review);
        void Delete(Review review);
        Task UpdateBookRatingAsync(int bookId);
        Task<bool> SaveChangesAsync();
    }
}