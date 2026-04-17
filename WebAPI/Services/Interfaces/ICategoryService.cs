using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResponse<object>> GetAllAsync(bool includeBookCount);
        Task<ApiResponse<CategoryDto>> GetByIdAsync(int id);
        Task<ApiResponse<object>> GetBooksAsync(int id, int page, int pageSize);
        Task<ApiResponse<CategoryDto>> CreateAsync(CategoryUpsertDto dto);
        Task<ApiResponse<CategoryDto>> UpdateAsync(int id, CategoryUpsertDto dto);
        Task<ApiResponse<object>> DeleteAsync(int id, bool force);
        Task<ApiResponse<List<CategoryDto>>> SearchAsync(string keyword);
    }
}