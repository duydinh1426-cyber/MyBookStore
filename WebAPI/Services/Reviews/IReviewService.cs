using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IReviewService
    {
        Task<ServiceResult<object>> GetByBookAsync(int bookId, int page, int pageSize, int? rating);
        Task<ServiceResult<object>> GetReviewStatusAsync(int userId, int bookId);
        Task<ServiceResult<object>> CreateAsync(int userId, CreateReviewDto dto);
        Task<ServiceResult> DeleteAsync(int id);
        Task<ServiceResult<object>> AdminGetAllAsync(int page, int pageSize, int? rating, int? bookId);
    }
}