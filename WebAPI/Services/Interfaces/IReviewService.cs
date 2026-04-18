using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IReviewService
    {
        Task<object> GetByBookAsync(int bookId, int page, int pageSize, int? rating);
        Task<object> GetReviewStatusAsync(int userId, int bookId);
        Task<object> CreateAsync(int userId, CreateReviewDto dto);
        Task<object> DeleteAsync(int id);
        Task<object> AdminGetAllAsync(int page, int pageSize, int? rating, int? bookId);
    }
}