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

        public BookService(IBookRepository repo)
        {
            _repo = repo; 
        }

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

        public async Task<ApiResponse<BookPagedResultDto>> GetBooks(BookQueryDto queryDto)
        {
            var query = _repo.GetQuery().AsNoTracking();

            // lọc
            if (!string.IsNullOrWhiteSpace(queryDto.Keyword))
            {
                var keyword = $"%{queryDto.Keyword.Trim()}%";
                query = query.Where(b => EF.Functions.Like(b.Title, keyword) || 
                                         EF.Functions.Like(b.Author, keyword));
            }

            if (queryDto.CategoryId.HasValue)
                query = query.Where(b => b.CategoryId == queryDto.CategoryId);

            if (queryDto.MinPrice.HasValue)
                query = query.Where(b => b.Price >= queryDto.MinPrice);

            if (queryDto.MaxPrice.HasValue)
                query = query.Where(b => b.Price <= queryDto.MaxPrice);

            // sorting
            query = (queryDto.SortBy.ToLower(), queryDto.SortOrder.ToLower()) switch
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

            return ApiResponse<BookPagedResultDto>.Success(new BookPagedResultDto
            (total, queryDto.Page, queryDto.PageSize, totalPages, data));
        }

        public async Task<ApiResponse<List<BookSummaryDto>>> GetTopNew(int count = 6)
        {
            var data = await _repo.GetQuery()
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .Select(b => MapToSummary(b))
                .ToListAsync();

            return ApiResponse<List<BookSummaryDto>>.Success(data);
        }

        public async Task<ApiResponse<List<BookSummaryDto>>> GetTopSelling(int count = 6)
        {
            var data = await _repo.GetQuery()
                .OrderByDescending(b => b.NumberSold)
                .Take(count)
                .Select(b => MapToSummary(b))
                .ToListAsync();
            return ApiResponse<List<BookSummaryDto>>.Success(data);
        }

        public async Task<ApiResponse<List<BookSummaryDto>>> GetTopRated(int count = 6)
        {
            var data = await _repo.GetQuery()
                .OrderByDescending(b => b.AvgRating)
                .ThenByDescending(b => b.ReviewCount)
                .Take(count)
                .Select(b => MapToSummary(b))
                .ToListAsync();

            return ApiResponse<List<BookSummaryDto>>.Success(data);
        }

        public async Task<ApiResponse<BookDetailDto>> GetById(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null)
                return ApiResponse<BookDetailDto>.Fail("Không tìm thấy sách", 404);

            return ApiResponse<BookDetailDto>.Success(new BookDetailDto(
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
                book.Category != null ? book.Category.CategoryName : null,
                (double)book.AvgRating,
                book.ReviewCount
            ));
        }

        public async Task<ApiResponse<object>> Create(BookUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return ApiResponse<object>.Fail("Tên sách không được để trống.");

            if (string.IsNullOrWhiteSpace(dto.Author))
                return ApiResponse<object>.Fail("Tên tác giả không được để trống.");

            if (dto.Price <= 0)
                return ApiResponse<object>.Fail("Giá sách phải lớn hơn 0.");

            if (dto.NumberStock < 0)
                return ApiResponse<object>.Fail("Số lượng tồn kho không được âm.");

            if (dto.CategoryId.HasValue && !await _repo.CategoryExists(dto.CategoryId.Value))
                 return ApiResponse<object>.Fail("Thể loại không tồn tại.");

            var book = new Book
            {
                CategoryId = dto.CategoryId,
                Author = dto.Author?.Trim() ?? "",
                Title = dto.Title.Trim(),
                PublisherYear = dto.PublisherYear,
                Description = dto.Description?.Trim(),
                Image = dto.Image?.Trim(),
                Price = dto.Price,
                NumberPage = dto.NumberPage ?? 0,
                NumberStock = dto.NumberStock,
                NumberSold = 0,
                AvgRating = 0,
                ReviewCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
             
            await _repo.AddAsync(book);

            return ApiResponse<object>.Success(
                new { bookId = book.BookId }, "Thêm sách thành công.");
        }

        public async Task<ApiResponse<object>> Update(int id, BookUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return ApiResponse<object>.Fail("Tên sách không được để trống.");

            if (string.IsNullOrWhiteSpace(dto.Author))
                return ApiResponse<object>.Fail("Tên tác giả không được để trống.");

            if (dto.Price <= 0)
                return ApiResponse<object>.Fail("Giá sách phải lớn hơn 0.");

            if (dto.NumberStock < 0)
                return ApiResponse<object>.Fail("Số lượng tồn kho không được âm.");

            if (dto.CategoryId.HasValue && !await _repo.CategoryExists(dto.CategoryId.Value))
                return ApiResponse<object>.Fail("Thể loại không tồn tại.");

            var book = await _repo.GetByIdAsync(id);
            if (book == null)
                return ApiResponse<object>.Fail("Không tìm thấy sách", 404);

            book.CategoryId = dto.CategoryId;
            book.Author = dto.Author?.Trim() ?? "";
            book.Title = dto.Title.Trim();
            book.PublisherYear = dto.PublisherYear;
            book.Description = dto.Description?.Trim();
            book.Image = dto.Image?.Trim();
            book.Price = dto.Price;
            book.NumberPage = dto.NumberPage ?? 0;
            book.NumberStock = dto.NumberStock;
            book.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(book);

            return ApiResponse<object>.Success(null, "Cập nhật sách thành công.");
        }

        public async Task<ApiResponse<object>> Delete(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null)
                return ApiResponse<object>.Fail("Không tìm thấy sách", 404);

            if (await _repo.HasOrderItems(id))
                return ApiResponse<object>.Fail("Không thể xóa sách đang có trong đơn hàng.");

            await _repo.DeleteAsync(book);

            return ApiResponse<object>.Success(null, "Xóa sách thành công.");
        }   
    }
}
