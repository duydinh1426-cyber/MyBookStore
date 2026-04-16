using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _repo;

    public ReviewService(IReviewRepository repo)
    {
        _repo = repo;
    }

    public async Task<object> GetByBook(int bookId, int page, int pageSize, int? rating)
    {
        if (!await _repo.BookExists(bookId))
            throw new Exception("Không tìm thấy sách");

        var query = _repo.GetReviewsByBook(bookId);

        if (rating.HasValue)
            query = query.Where(r => r.Rating == rating.Value);

        var total = await _repo.CountReviews(query);
        var avg = await _repo.GetAverageRating(query);
        var stats = await _repo.GetRatingStats(bookId);
        var reviews = await _repo.GetPagedReviews(query, page, pageSize);

        var data = reviews.Select(r => new ReviewResponseDto(
            r.ReviewId,
            r.User.Name ?? "",
            r.Rating,
            r.Comment,
            r.CreatedAt
        ));

        return new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            avgRating = Math.Round(avg, 1),
            ratingStats = stats,
            data
        };
    }

    public async Task<object> GetReviewStatus(int userId, int bookId)
    {
        if (!await _repo.HasPurchased(userId, bookId))
            return new { canReview = false, reason = "not_purchased" };

        var review = await _repo.GetUserReview(userId, bookId);

        if (review != null)
        {
            return new
            {
                canReview = false,
                reason = "already_reviewed",
                review.Rating,
                review.Comment
            };
        }

        return new { canReview = true };
    }

    public async Task<object> Create(int userId, CreateReviewDto dto)
    {
        if (dto.rating < 1 || dto.rating > 5)
            throw new Exception("Rating phải từ 1-5");

        if (!await _repo.BookExists(dto.bookId))
            throw new Exception("Không tìm thấy sách");

        if (!await _repo.HasPurchased(userId, dto.bookId))
            throw new Exception("Bạn cần mua sách trước");

        if (await _repo.AlreadyReviewed(userId, dto.bookId))
            throw new Exception("Bạn đã đánh giá rồi");

        var review = new Review
        {
            BookId = dto.bookId,
            UserId = userId,
            Rating = dto.rating,
            Comment = dto.comment?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.AddReview(review);
        await _repo.SaveAsync();

        await _repo.UpdateBookRating(dto.bookId);
        await _repo.SaveAsync();

        return new
        {
            message = "Đã tạo review",
            reviewId = review.ReviewId
        };
    }

    public async Task<object> Delete(int id)
    {
        var review = await _repo.GetById(id);
        if (review == null)
            throw new Exception("Không tìm thấy");

        var bookId = review.BookId;

        await _repo.DeleteReview(review);

        await _repo.UpdateBookRating(bookId);
        await _repo.SaveAsync();

        return new { message = "Đã xóa" };
    }

    public async Task<object> AdminGetAll(int page, int pageSize, int? rating, int? bookId)
    {
        var query = _repo.GetAll();

        if (rating.HasValue)
            query = query.Where(r => r.Rating == rating.Value);

        if (bookId.HasValue)
            query = query.Where(r => r.BookId == bookId.Value);

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.ReviewId,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                customer = r.User.Name,
                book = new { r.Book.BookId, r.Book.Title }
            })
            .ToListAsync();

        return new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data
        };
    }
}