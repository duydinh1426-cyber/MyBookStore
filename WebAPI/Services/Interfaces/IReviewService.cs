using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IReviewService
    {
        Task<object> GetByBook(int bookId, int page, int pageSize, int? rating);

        Task<object> GetReviewStatus(int userId, int bookId);

        Task<object> Create(int userId, CreateReviewDto dto);

        Task<object> Delete(int id);

        Task<object> AdminGetAll(int page, int pageSize, int? rating, int? bookId);
    }
}
