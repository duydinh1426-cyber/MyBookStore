using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        Task<bool> BookExists(int bookId); // kiểm tra sách có tồn tại không
        IQueryable<Review> GetReviewsByBook(int bookId); // lấy ds review của một sách
        Task<int> CountReviews(IQueryable<Review> query); // đếm tổng số review
        Task<List<Review>> GetPagedReviews(IQueryable<Review> query, int page, int pageSize); // lấy ds review theo phân trang
        Task<double> GetAverageRating(IQueryable<Review> query); // tính điểm tb
        Task<Dictionary<int, int>> GetRatingStats(int bookId); // thống kê theo số sao
        Task<bool> HasPurchased(int userId, int bookId); //Kiểm tra đã mua sách chưa
        Task<Review?> GetUserReview(int userId, int bookId);
        Task<bool> AlreadyReviewed(int userId, int bookId); // Kiểm tra user đã review chưa
        Task AddReview(Review review); 
        Task<Review?> GetById(int id);
        Task DeleteReview(Review review);
        Task SaveAsync();
        Task UpdateBookRating(int  bookId); // Cập nhật lại rating trung bình
        IQueryable<Review> GetAll(); // Lấy tất cả review
    }
}
