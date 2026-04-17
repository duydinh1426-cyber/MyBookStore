using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _repo;
        public ReviewService(IReviewRepository repo) => _repo = repo;

        public async Task<ApiResponse<object>> GetByBookAsync(int bookId, int page, int pageSize, int? rating)
        {
            if (!await _repo.BookExistsAsync(bookId))
                return ApiResponse<object>.Fail("Không tìm thấy sách.", 404);

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
                    r.ReviewId,
                    CustomerName = r.User.Name,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                }).ToListAsync();

            return ApiResponse<object>.Success(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                AvgRating = Math.Round(avg, 1),
                RatingStats = stats,
                Data = items
            });
        }

        public async Task<ApiResponse<object>> GetReviewStatusAsync(int userId, int bookId)
        {
            if (!await _repo.HasPurchasedAsync(userId, bookId))
                return ApiResponse<object>.Success(new { CanReview = false, Reason = "not_purchased" });

            var review = await _repo.GetUserReviewAsync(userId, bookId);
            if (review != null)
                return ApiResponse<object>.Success(new
                {
                    CanReview = false,
                    Reason = "already_reviewed",
                    Rating = review.Rating,
                    Comment = review.Comment
                });

            return ApiResponse<object>.Success(new { CanReview = true });
        }

        public async Task<ApiResponse<object>> CreateAsync(int userId, CreateReviewDto dto)
        {
            if (dto.rating < 1 || dto.rating > 5) return ApiResponse<object>.Fail("Đánh giá phải từ 1 đến 5 sao.");
            if (!await _repo.BookExistsAsync(dto.bookId)) return ApiResponse<object>.Fail("Sách không tồn tại.", 404);
            if (!await _repo.HasPurchasedAsync(userId, dto.bookId)) return ApiResponse<object>.Fail("Bạn cần mua sản phẩm này trước khi đánh giá.");
            if (await _repo.GetUserReviewAsync(userId, dto.bookId) != null) return ApiResponse<object>.Fail("Bạn đã đánh giá sản phẩm này rồi.");

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

            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(new { reviewId = review.ReviewId }, "Gửi đánh giá thành công.")
                : ApiResponse<object>.Fail("Lỗi khi lưu đánh giá.", 500);
        }

        public async Task<ApiResponse<object>> DeleteAsync(int id)
        {
            var review = await _repo.GetByIdAsync(id);
            if (review == null) return ApiResponse<object>.Fail("Không tìm thấy đánh giá.", 404);

            var bookId = review.BookId;
            _repo.Delete(review);
            await _repo.UpdateBookRatingAsync(bookId);

            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(null, "Đã xóa đánh giá.")
                : ApiResponse<object>.Fail("Lỗi khi xóa đánh giá.", 500);
        }

        public async Task<ApiResponse<object>> AdminGetAllAsync(int page, int pageSize, int? rating, int? bookId)
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
                    r.ReviewId,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    CustomerName = r.User.Name,
                    Book = new { r.Book.BookId, r.Book.Title }
                }).ToListAsync();

            return ApiResponse<object>.Success(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = items
            });
        }
    }
}