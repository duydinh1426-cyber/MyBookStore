using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;

namespace WebAPI.Services.Books
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _repo;
        private readonly ICategoryRepository _categoryRepository;

        public BookService(IBookRepository repo, ICategoryRepository categoryRepository)
        {
            _repo = repo;
            _categoryRepository = categoryRepository;
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
            var (total, page, pageSize, totalPages, books) = await _repo.GetBookAsync(
                queryDto.Page,
                queryDto.PageSize,
                queryDto.Keyword,
                queryDto.CategoryId,
                queryDto.MinPrice,
                queryDto.MaxPrice,
                queryDto.SortBy ?? "createdAt",
                queryDto.SortOrder ?? "desc"
            );

            var data = books.Select(b => MapToSummary(b)).ToList();

            return new BookPagedResultDto(total, page, pageSize, totalPages, data);
        }

        public async Task<List<BookSummaryDto>> GetTopNewAsync(int count)
        {
            var books = await _repo.GetTopNewAsync(count);
            return books.Select(b => MapToSummary(b)).ToList();
        }

        public async Task<List<BookSummaryDto>> GetTopSellingAsync(int count)
        {
            var books = await _repo.GetTopSellingAsync(count);
            return books.Select(b => MapToSummary(b)).ToList();
        }

        public async Task<List<BookSummaryDto>> GetTopRatedAsync(int count)
        {
            var books = await _repo.GetTopRatedAsync(count);
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
            if (dto.CategoryId.HasValue && !await _categoryRepository.ExistsByIdAsync(dto.CategoryId.Value))
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

            if (dto.CategoryId.HasValue && !await _categoryRepository.ExistsByIdAsync(dto.CategoryId.Value))
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