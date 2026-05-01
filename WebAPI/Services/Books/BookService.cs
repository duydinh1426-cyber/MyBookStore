using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;
using WebAPI.Services.Helper;

namespace WebAPI.Services.Books
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

        public async Task<ServiceResult<BookPagedResultDto>> GetBooksAsync(BookQueryDto queryDto)
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

            var result = new BookPagedResultDto(total, queryDto.Page, queryDto.PageSize, totalPages, data);
            return ServiceResult<BookPagedResultDto>.Success(result);
        }

        public async Task<ServiceResult<List<BookSummaryDto>>> GetTopNewAsync(int count)
        {
            var books = await _repo.GetQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();

            return ServiceResult<List<BookSummaryDto>>.Success(books.Select(b => MapToSummary(b)).ToList());
        }

        public async Task<ServiceResult<List<BookSummaryDto>>> GetTopSellingAsync(int count)
        {
            var books = await _repo.GetQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.NumberSold)
                .Take(count)
                .ToListAsync();

            return ServiceResult<List<BookSummaryDto>>.Success(books.Select(b => MapToSummary(b)).ToList());
        }

        public async Task<ServiceResult<List<BookSummaryDto>>> GetTopRatedAsync(int count)
        {
            var books = await _repo.GetQuery()
                .AsNoTracking()
                .Where(b => b.ReviewCount > 0)
                .OrderByDescending(b => b.AvgRating)
                .ThenByDescending(b => b.ReviewCount)
                .Take(count)
                .ToListAsync();

            return ServiceResult<List<BookSummaryDto>>.Success(books.Select(b => MapToSummary(b)).ToList());
        }

        public async Task<ServiceResult<BookDetailDto>> GetByIdAsync(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null)
                return ServiceResult<BookDetailDto>.Failure("Không tìm thấy sách.");

            var data = new BookDetailDto(
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

            return ServiceResult<BookDetailDto>.Success(data);
        }

        public async Task<ServiceResult<int>> CreateAsync(BookUpsertDto dto)
        {
            if (dto.CategoryId.HasValue && !await _repo.CategoryExistsAsync(dto.CategoryId.Value))
                return ServiceResult<int>.Failure("Thể loại không tồn tại.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return ServiceResult<int>.Failure("Tên sách không được để trống.");

            if (dto.Price <= 0)
                return ServiceResult<int>.Failure("Giá sách phải lớn hơn 0.");

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
                CreatedAt = TimeHelper.NowVietnam(),
                UpdatedAt = TimeHelper.NowVietnam()
            };

            _repo.Add(book);
            var success = await _repo.SaveChangesAsync();

            if (!success)
                return ServiceResult<int>.Failure("Lỗi hệ thống.", 500);

            return ServiceResult<int>.Success(book.BookId, "Thêm sách thành công.");
        }

        public async Task<ServiceResult> UpdateAsync(int id, BookUpsertDto dto)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null) 
                return ServiceResult.Failure("Không tìm thấy sách.");

            if (dto.CategoryId.HasValue && !await _repo.CategoryExistsAsync(dto.CategoryId.Value))
                return ServiceResult.Failure("Thể loại không tồn tại.");
            

            book.CategoryId = dto.CategoryId;
            book.Author = dto.Author?.Trim();
            book.Title = dto.Title.Trim();
            book.PublisherYear = dto.PublisherYear;
            book.Description = dto.Description?.Trim();
            book.Image = dto.Image?.Trim();
            book.Price = dto.Price;
            book.NumberPage = dto.NumberPage;
            book.NumberStock = dto.NumberStock;
            book.UpdatedAt = TimeHelper.NowVietnam();

            _repo.Update(book);
            var success = await _repo.SaveChangesAsync();

            if (!success)
                return ServiceResult.Failure("Lỗi hệ thống.", 500);

            return ServiceResult.Success("Cập nhật sách thành công.");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null) 
                return ServiceResult.Failure("Không tìm thấy sách.");

            if (await _repo.HasOrderItemsAsync(id))
                return ServiceResult.Failure("Không thể xóa sách đã có trong đơn hàng.");
            

            _repo.Delete(book);
            var success = await _repo.SaveChangesAsync();

            if (!success)
                return ServiceResult.Failure("Lỗi hệ thống.", 500);

            return ServiceResult.Success("Xóa sách thành công.");
        }
    }
}