using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IBookService
    {
        Task<ServiceResult<BookPagedResultDto>> GetBooksAsync(BookQueryDto queryDto);
        Task<ServiceResult<List<BookSummaryDto>>> GetTopNewAsync(int count);
        Task<ServiceResult<List<BookSummaryDto>>> GetTopSellingAsync(int count);
        Task<ServiceResult<List<BookSummaryDto>>> GetTopRatedAsync(int count);
        Task<ServiceResult<BookDetailDto>> GetByIdAsync(int id);
        Task<ServiceResult<int>> CreateAsync(BookUpsertDto dto);
        Task<ServiceResult> UpdateAsync(int id, BookUpsertDto dto);
        Task<ServiceResult> DeleteAsync(int id);
    }
}