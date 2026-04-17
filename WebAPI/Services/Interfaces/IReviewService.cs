using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IReviewService
    {
        Task<ApiResponse<object>> GetByBookAsync(int bookId, int page, int pageSize, int? rating);
        Task<ApiResponse<object>> GetReviewStatusAsync(int userId, int bookId);
        Task<ApiResponse<object>> CreateAsync(int userId, CreateReviewDto dto);
        Task<ApiResponse<object>> DeleteAsync(int id);
        Task<ApiResponse<object>> AdminGetAllAsync(int page, int pageSize, int? rating, int? bookId);
    }
}