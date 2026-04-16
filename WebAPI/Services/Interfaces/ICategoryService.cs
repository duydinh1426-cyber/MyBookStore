using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResponse<object>> GetAll(bool includeBookCount);
        Task<ApiResponse<CategoryDto?>> GetById(int id);
        Task<ApiResponse<object>> GetBooks(int id, int page, int pageSize);

        Task<ApiResponse<CategoryDto>> Create(CategoryUpsertDto dto);
        Task<ApiResponse<CategoryDto>> Update(int id, CategoryUpsertDto dto);
        Task<ApiResponse<object>> Delete(int id, bool force);

        Task<ApiResponse<List<CategoryDto>>> Search(string keyword);
    }
}
