using WebAPI.DTOs;

namespace WebAPI.Services.Books
{
    public interface IBookService
    {
        Task<BookPagedResultDto> GetBooksAsync(BookQueryDto queryDto);
        Task<List<BookSummaryDto>> GetTopNewAsync(int count);
        Task<List<BookSummaryDto>> GetTopSellingAsync(int count);
        Task<List<BookSummaryDto>> GetTopRatedAsync(int count);
        Task<BookDetailDto?> GetByIdAsync(int id);
        Task<(string? Error, int? BookId)> CreateAsync(BookUpsertDto dto);
        Task<string?> UpdateAsync(int id, BookUpsertDto dto);
        Task<string?> DeleteAsync(int id);
    }
}