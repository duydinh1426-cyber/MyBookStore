using Data;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
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

        private BookSummaryDto MapToSummary(Book book)
        {
            return new BookSummaryDto(
                book.BookId,
                book.Title,
                book.Author,
                book.Price,
                book.Image,
                book.Category?.CategoryName,
                book.NumberStock,
                book.NumberSold
            );
        }

        public async Task<BookPagedResultDto> GetBooksAsync(BookQueryDto queryDto)
        {
            var query = _repo.GetQuery().AsNoTracking();

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

            var books = await query
                .Skip((queryDto.Page - 1) * queryDto.PageSize)
                .Take(queryDto.PageSize)
                .ToListAsync();

            var data = books.Select(b => MapToSummary(b)).ToList();

            return new BookPagedResultDto(total, queryDto.Page, queryDto.PageSize, totalPages, data);
        }

        public async Task<List<BookSummaryDto>> GetTopNewAsync(int count)
        {
            var books = await _repo.GetQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();

            return books.Select(b => MapToSummary(b)).ToList();
        }

        public async Task<List<BookSummaryDto>> GetTopSellingAsync(int count)
        {
            var books = await _repo.GetQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.NumberSold)
                .Take(count)
                .ToListAsync();

            return books.Select(b => MapToSummary(b)).ToList();
        }

        public async Task<List<BookSummaryDto>> GetTopRatedAsync(int count)
        {
            var books = await _repo.GetQuery()
                .AsNoTracking()
                .Where(b => b.ReviewCount > 0)
                .OrderByDescending(b => b.AvgRating)
                .ThenByDescending(b => b.ReviewCount)
                .Take(count)
                .ToListAsync();

            return books.Select(b => MapToSummary(b)).ToList();
        }

        public async Task<BookDetailDto?> GetByIdAsync(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null) return null;

            return new BookDetailDto(
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
                Math.Round((double)book.AvgRating, 1),
                book.ReviewCount
            );
        }

        public async Task<(string? Error, int? BookId)> CreateAsync(BookUpsertDto dto)
        {
            if (dto.CategoryId.HasValue && !await _repo.CategoryExistsAsync(dto.CategoryId.Value))
            {
                return ("Thể loại không tồn tại.", null);
            }

            var book = new Book
            {
                CategoryId = dto.CategoryId,
                Author = dto.Author?.Trim(),
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
            var success = await _repo.SaveChangesAsync();

            if (success) return (null, book.BookId);
            return ("Lỗi hệ thống khi lưu sách.", null);
        }

        public async Task<string?> UpdateAsync(int id, BookUpsertDto dto)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null) return "Không tìm thấy sách.";

            if (dto.CategoryId.HasValue && !await _repo.CategoryExistsAsync(dto.CategoryId.Value))
            {
                return "Thể loại không tồn tại.";
            }

            book.CategoryId = dto.CategoryId;
            book.Author = dto.Author?.Trim();
            book.Title = dto.Title.Trim();
            book.PublisherYear = dto.PublisherYear;
            book.Description = dto.Description?.Trim();
            book.Image = dto.Image?.Trim();
            book.Price = dto.Price;
            book.NumberPage = dto.NumberPage;
            book.NumberStock = dto.NumberStock;
            book.UpdatedAt = DateTime.UtcNow;

            _repo.Update(book);
            var success = await _repo.SaveChangesAsync();
            return success ? null : "Lỗi hệ thống khi cập nhật sách.";
        }

        public async Task<string?> DeleteAsync(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null) return "Không tìm thấy sách.";

            if (await _repo.HasOrderItemsAsync(id))
            {
                return "Không thể xóa sách đã có trong đơn hàng.";
            }

            _repo.Delete(book);
            var success = await _repo.SaveChangesAsync();
            return success ? null : "Lỗi hệ thống khi xóa sách.";
        }
    }
}