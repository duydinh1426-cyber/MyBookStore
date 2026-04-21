using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _repo;
        public ReviewService(IReviewRepository repo) => _repo = repo;

        public async Task<object> GetByBookAsync(int bookId, int page, int pageSize, int? rating)
        {
            if (!await _repo.BookExistsAsync(bookId))
                return new { message = "NotFound" };

            var query = _repo.GetQuery().Where(r => r.BookId == bookId);
            if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);

            var total = await query.CountAsync();
            var avg = total > 0 ? await query.AverageAsync(r => r.Rating) : 0;
            var stats = await _repo.GetRatingStatsAsync(bookId);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new {
                    reviewId = r.ReviewId,
                    name = r.User.Name, 
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt
                }).ToListAsync();

            return new
            {
                message = "",
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                avgRating = Math.Round(avg, 1),
                ratingStats = stats,
                data = items
            };
        }

        public async Task<object> GetReviewStatusAsync(int userId, int bookId)
        {
            if (!await _repo.HasPurchasedAsync(userId, bookId))
                return new { canReview = false, reason = "not_purchased" };

            var review = await _repo.GetUserReviewAsync(userId, bookId);
            if (review != null)
                return new
                {
                    canReview = false,
                    reason = "already_reviewed",
                    rating = review.Rating,
                    comment = review.Comment
                };

            return new { canReview = true };
        }

        public async Task<object> CreateAsync(int userId, CreateReviewDto dto)
        {
            if (dto.rating < 1 || dto.rating > 5)
                return new { message = "Đánh giá phải từ 1 đến 5 sao." };

            if (!await _repo.BookExistsAsync(dto.bookId))
                return new { message = "NotFound" };

            if (!await _repo.HasPurchasedAsync(userId, dto.bookId))
                return new { message = "Bạn cần mua sản phẩm này trước khi đánh giá." };

            if (await _repo.GetUserReviewAsync(userId, dto.bookId) != null)
                return new { message = "Bạn đã đánh giá sản phẩm này rồi." };

            var review = new Review
            {
                BookId = dto.bookId,
                UserId = userId,
                Rating = dto.rating,
                Comment = dto.comment?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _repo.Add(review);
            await _repo.UpdateBookRatingAsync(dto.bookId);

            if (await _repo.SaveChangesAsync())
                return new { message = "Gửi đánh giá thành công.", reviewId = review.ReviewId };

            return new { message = "Lỗi khi lưu đánh giá." };
        }

        public async Task<object> DeleteAsync(int id)
        {
            var review = await _repo.GetByIdAsync(id);
            if (review == null) return new { message = "NotFound" };

            var bookId = review.BookId;
            _repo.Delete(review);
            await _repo.UpdateBookRatingAsync(bookId);

            if (await _repo.SaveChangesAsync()) return new { message = "Đã xóa đánh giá." };
            return new { message = "Lỗi khi xóa đánh giá." };
        }

        public async Task<object> AdminGetAllAsync(int page, int pageSize, int? rating, int? bookId)
        {
            var query = _repo.GetQuery();
            if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);
            if (bookId.HasValue) query = query.Where(r => r.BookId == bookId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new {
                    reviewId = r.ReviewId,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt,
                    customer = r.User.Name,
                    book = new { bookId = r.Book.BookId, title = r.Book.Title }
                }).ToListAsync();

            return new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                data = items
            };
        }
    }
}