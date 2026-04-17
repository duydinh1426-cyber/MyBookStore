using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
using System.Globalization;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _repo;

        public BookService(IBookRepository repo) => _repo = repo;

        private static BookSummaryDto MapToSummary(Book book)
        {
            return new BookSummaryDto(
                book.BookId,
                book.Title,
                book.Author,
                book.Price,
                book.Image,
                book.Category != null ? book.Category.CategoryName : null,
                book.NumberStock,
                book.NumberSold
            );
        }

        public async Task<ApiResponse<BookPagedResultDto>> GetBooksAsync(BookQueryDto queryDto)
        {
            var query = _repo.GetQuery().AsNoTracking();

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(queryDto.Keyword))
            {
                var keyword = queryDto.Keyword.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(keyword) ||
                        (b.Author != null && b.Author.ToLower().Contains(keyword)));
            }

            if (queryDto.CategoryId.HasValue)
                query = query.Where(b => b.CategoryId == queryDto.CategoryId);

            if (queryDto.MinPrice.HasValue)
                query = query.Where(b => b.Price >= queryDto.MinPrice);

            if (queryDto.MaxPrice.HasValue)
                query = query.Where(b => b.Price <= queryDto.MaxPrice);

            // Sắp xếp
            query = (queryDto.SortBy?.ToLower(), queryDto.SortOrder?.ToLower()) switch
            {
                ("price", "asc") => query.OrderBy(b => b.Price),
                ("price", _) => query.OrderByDescending(b => b.Price),
                ("title", "asc") => query.OrderBy(b => b.Title),
                ("title", _) => query.OrderByDescending(b => b.Title),
                ("numbersold", "asc") => query.OrderBy(b => b.NumberSold),
                ("numbersold", _) => query.OrderByDescending(b => b.NumberSold),
                ("avgrating", "asc") => query.OrderBy(b => b.AvgRating),
                ("avgrating", _) => query.OrderByDescending(b => b.AvgRating),
                (_, "asc") => query.OrderBy(b => b.CreatedAt),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)total / queryDto.PageSize);

            var data = await query
                .Skip((queryDto.Page - 1) * queryDto.PageSize)
                .Take(queryDto.PageSize)
                .Select(b => MapToSummary(b))
                .ToListAsync();

            var result = new BookPagedResultDto(total, queryDto.Page, queryDto.PageSize, totalPages, data);
            return ApiResponse<BookPagedResultDto>.Success(result);
        }

        public async Task<ApiResponse<List<BookSummaryDto>>> GetTopNewAsync(int count)
        {
            var data = await _repo.GetQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .Select(b => MapToSummary(b))
                .ToListAsync();

            return ApiResponse<List<BookSummaryDto>>.Success(data);
        }

        public async Task<ApiResponse<List<BookSummaryDto>>> GetTopSellingAsync(int count)
        {
            var data = await _repo.GetQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.NumberSold)
                .Take(count)
                .Select(b => MapToSummary(b))
                .ToListAsync();

            return ApiResponse<List<BookSummaryDto>>.Success(data);
        }

        public async Task<ApiResponse<List<BookSummaryDto>>> GetTopRatedAsync(int count)
        {
            var data = await _repo.GetQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.AvgRating)
                .ThenByDescending(b => b.ReviewCount)
                .Take(count)
                .Select(b => MapToSummary(b))
                .ToListAsync();

            return ApiResponse<List<BookSummaryDto>>.Success(data);
        }

        public async Task<ApiResponse<BookDetailDto>> GetByIdAsync(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null)
                return ApiResponse<BookDetailDto>.Fail("Không tìm thấy sách.", 404);

            var detail = new BookDetailDto(
                book.BookId,
                book.Title ?? "",
                book.Author ?? "",
                book.Price,
                book.Image,
                book.Description,
                book.PublisherYear,
                book.NumberPage ?? 0,
                book.NumberStock,
                book.NumberSold,
                book.CategoryId,
                book.Category?.CategoryName,
                (double)book.AvgRating,
                book.ReviewCount
            );

            return ApiResponse<BookDetailDto>.Success(detail);
        }

        public async Task<ApiResponse<object>> CreateAsync(BookUpsertDto dto)
        {
            if (dto.CategoryId.HasValue && !await _repo.CategoryExistsAsync(dto.CategoryId.Value))
                return ApiResponse<object>.Fail("Thể loại không tồn tại.");

            var book = new Book
            {
                CategoryId = dto.CategoryId,
                Author = (dto.Author ?? "").Trim(),
                Title = dto.Title.Trim(),
                PublisherYear = dto.PublisherYear,
                Description = dto.Description?.Trim(),
                Image = dto.Image?.Trim(),
                Price = dto.Price,
                NumberPage = dto.NumberPage,
                NumberStock = dto.NumberStock,
                NumberSold = 0,
                AvgRating = 0,
                ReviewCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _repo.Add(book);
            if (await _repo.SaveChangesAsync())
                return ApiResponse<object>.Success(new { bookId = book.BookId }, "Thêm sách thành công.");

            return ApiResponse<object>.Fail("Lỗi hệ thống khi lưu sách.", 500);
        }

        public async Task<ApiResponse<object>> UpdateAsync(int id, BookUpsertDto dto)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null)
                return ApiResponse<object>.Fail("Không tìm thấy sách.", 404);

            if (dto.CategoryId.HasValue && !await _repo.CategoryExistsAsync(dto.CategoryId.Value))
                return ApiResponse<object>.Fail("Thể loại không tồn tại.");

            book.CategoryId = dto.CategoryId;
            book.Author = (dto.Author ?? "").Trim();
            book.Title = dto.Title.Trim();
            book.PublisherYear = dto.PublisherYear;
            book.Description = dto.Description?.Trim();
            book.Image = dto.Image?.Trim();
            book.Price = dto.Price;
            book.NumberPage = dto.NumberPage;
            book.NumberStock = dto.NumberStock;
            book.UpdatedAt = DateTime.UtcNow;

            _repo.Update(book);
            if (await _repo.SaveChangesAsync())
                return ApiResponse<object>.Success(null, "Cập nhật sách thành công.");

            return ApiResponse<object>.Fail("Lỗi hệ thống khi cập nhật sách.", 500);
        }

        public async Task<ApiResponse<object>> DeleteAsync(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null)
                return ApiResponse<object>.Fail("Không tìm thấy sách.", 404);

            if (await _repo.HasOrderItemsAsync(id))
                return ApiResponse<object>.Fail("Không thể xóa sách đã có trong đơn hàng.");

            _repo.Delete(book);
            if (await _repo.SaveChangesAsync())
                return ApiResponse<object>.Success(null, "Xóa sách thành công.");

            return ApiResponse<object>.Fail("Lỗi hệ thống khi xóa sách.", 500);
        }
    }
}
