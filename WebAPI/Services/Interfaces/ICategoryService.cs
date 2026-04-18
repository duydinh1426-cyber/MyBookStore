using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<object> GetAllAsync(bool includeBookCount);
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<object?> GetBooksAsync(int id, int page, int pageSize);
        Task<object> CreateAsync(CategoryUpsertDto dto);
        Task<object> UpdateAsync(int id, CategoryUpsertDto dto);
        Task<object> DeleteAsync(int id, bool force);
        Task<List<CategoryDto>> SearchAsync(string keyword);
    }
}