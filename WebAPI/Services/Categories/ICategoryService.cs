using WebAPI.DTOs;

namespace WebAPI.Services.Categories
{
    public interface ICategoryService
    {
        Task<ServiceResult<List<CategoryDto>>> GetAllAsync();
        Task<ServiceResult<CategoryDto>> GetByIdAsync(int id);
        Task<ServiceResult<object>> GetBooksAsync(int id, int page, int pageSize);
        Task<ServiceResult<CategoryDto>> CreateAsync(CategoryUpsertDto dto);
        Task<ServiceResult<CategoryDto>> UpdateAsync(int id, CategoryUpsertDto dto);
        Task<ServiceResult> DeleteAsync(int id);
        Task<ServiceResult<List<CategoryDto>>> SearchAsync(string keyword);
    }
}