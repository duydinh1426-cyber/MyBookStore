using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IBookService
    {
        Task<ApiResponse<BookPagedResultDto>> GetBooks(BookQueryDto query);
        Task<ApiResponse<List<BookSummaryDto>>> GetTopNew(int count = 6);
        Task<ApiResponse<List<BookSummaryDto>>> GetTopSelling(int count = 6);
        Task<ApiResponse<List<BookSummaryDto>>> GetTopRated(int count = 6);
        Task<ApiResponse<BookDetailDto>> GetById(int id);
        Task<ApiResponse<object>> Create(BookUpsertDto dto);
        Task<ApiResponse<object>> Update(int id, BookUpsertDto dto);
        Task<ApiResponse<object>> Delete(int id);
    }
}
