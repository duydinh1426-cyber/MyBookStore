using Data.Repositories.Interfaces;
using MyBookStore.Data.Models;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;

        public CategoryService(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiResponse<CategoryDto>> Create(CategoryUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                return ApiResponse<CategoryDto>.Fail("Tên thể loại không được để trống.");
            
            if (await _repo.ExistsByName(dto.CategoryName))
                return ApiResponse<CategoryDto>.Fail("Thể loại đã tồn tại.");

            var cat = new Category
            {
                CategoryName = dto.CategoryName.Trim()
            };

            await _repo.AddAsync(cat);

            var cate = new CategoryDto(cat.CategoryId, cat.CategoryName);
            
            return ApiResponse<CategoryDto>.Success(cate, "Thể loại đã được tạo thành công.");
        }

        public async Task<ApiResponse<object>> Delete(int id, bool force)
        {
            var cat = await _repo.GetCategoryByIdAsync(id);
            if (cat == null)
                return ApiResponse<object>.Fail("Không tìm thấy thể loại.");

            if (cat.Books != null && cat.Books.Any() && !force)
                return ApiResponse<object>.Fail($"Thể loại còn {cat.Books.Count} sách. Dùng force=true để xóa.");

            await _repo.DeleteAsync(cat);

            return ApiResponse<object>.Success(null, "Đã xóa thành công.");
        }

        public async Task<ApiResponse<object>> GetAll(bool includeBookCount)
        {
            var cats = await _repo.GetAllCategoriesAsync();

            if (includeBookCount)
            {
                var result = cats.Select(c => new
                {
                    categoryId = c.CategoryId,
                    categoryName = c.CategoryName,
                    bookCount = c.Books?.Count ?? 0
                });

                return ApiResponse<object>.Success(result);
            }

            var data = cats.Select(c => new CategoryDto(c.CategoryId, c.CategoryName));
            return ApiResponse<object>.Success(data);
        }

        public async Task<ApiResponse<object>> GetBooks(int id, int page, int pageSize)
        {
            if (!await _repo.Exists(id))
                return ApiResponse<object>.Fail("Không tìm thấy thể loại.");

            var (total, books) = await _repo.GetBooksByCategory(id, page, pageSize);

            var result = new
            {
                total, page, pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                data = books.Select(b => new BookSummaryDto(
                    b.BookId,
                    b.Title,
                    b.Author,
                    b.Price,
                    b.Image,
                    b.Category?.CategoryName,
                    b.NumberStock,
                    b.NumberSold)
                )
            };
            
            return ApiResponse<object>.Success(result);
        }

        public async Task<ApiResponse<CategoryDto?>> GetById(int id)
        {
            var cat = await _repo.GetCategoryByIdAsync(id);
            if (cat == null)
                return ApiResponse<CategoryDto?>.Fail("Không tìm thấy thể loại.");
            
            return ApiResponse<CategoryDto?>.Success(
                new CategoryDto(cat.CategoryId, cat.CategoryName));
        }

        public async Task<ApiResponse<List<CategoryDto>>> Search(string keyword)
        {
            var cats = await _repo.Search(keyword);

            var data = cats.Select(c =>
                new CategoryDto(c.CategoryId, c.CategoryName)).ToList();

            return ApiResponse<List<CategoryDto>>.Success(data);
        }

        public async Task<ApiResponse<CategoryDto>> Update(int id, CategoryUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                return ApiResponse<CategoryDto>.Fail("Tên thể loại không được để trống.");

            var cat = await _repo.GetCategoryByIdAsync(id);
            if (cat == null)
                return ApiResponse<CategoryDto>.Fail("Không tìm thấy thể loại.");

            if (await _repo.ExistsByName(dto.CategoryName, id))
                return ApiResponse<CategoryDto>.Fail("Tên thể loại đã tồn tại.");

            cat.CategoryName = dto.CategoryName.Trim();

            await _repo.UpdateAsync(cat);
            
            return ApiResponse<CategoryDto>.Success(
                new CategoryDto(cat.CategoryId, cat.CategoryName),
                "Cập nhật thể loại thành công.");

        }
    }
}
