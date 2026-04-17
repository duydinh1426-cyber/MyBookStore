using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IBookService
    {
        Task<ApiResponse<BookPagedResultDto>> GetBooksAsync(BookQueryDto queryDto);
        Task<ApiResponse<List<BookSummaryDto>>> GetTopNewAsync(int count);
        Task<ApiResponse<List<BookSummaryDto>>> GetTopSellingAsync(int count);
        Task<ApiResponse<List<BookSummaryDto>>> GetTopRatedAsync(int count);
        Task<ApiResponse<BookDetailDto>> GetByIdAsync(int id);
        Task<ApiResponse<object>> CreateAsync(BookUpsertDto dto);
        Task<ApiResponse<object>> UpdateAsync(int id, BookUpsertDto dto);
        Task<ApiResponse<object>> DeleteAsync(int id);
    }
}
