using Data.Repositories.Interfaces;
using MyBookStore.Data.Models;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        public CategoryService(ICategoryRepository repo) => _repo = repo;

        public async Task<ApiResponse<object>> GetAllAsync(bool includeBookCount)
        {
            var cats = await _repo.GetAllAsync();

            if (includeBookCount)
            {
                var result = cats.Select(c => new {
                    c.CategoryId,
                    c.CategoryName,
                    BookCount = c.Books?.Count ?? 0
                });
                return ApiResponse<object>.Success(result);
            }

            var data = cats.Select(c => new CategoryDto(c.CategoryId, c.CategoryName));
            return ApiResponse<object>.Success(data);
        }

        public async Task<ApiResponse<CategoryDto>> GetByIdAsync(int id)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null) return ApiResponse<CategoryDto>.Fail("Không tìm thấy thể loại.", 404);

            return ApiResponse<CategoryDto>.Success(new CategoryDto(cat.CategoryId, cat.CategoryName));
        }

        public async Task<ApiResponse<object>> GetBooksAsync(int id, int page, int pageSize)
        {
            if (!await _repo.ExistsByIdAsync(id))
                return ApiResponse<object>.Fail("Thể loại không tồn tại.", 404);

            var (total, books) = await _repo.GetBooksByCategoryAsync(id, page, pageSize);

            var result = new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                Data = books.Select(b => new BookSummaryDto(
                    b.BookId, b.Title, b.Author, b.Price, b.Image,
                    null, b.NumberStock, b.NumberSold))
            };

            return ApiResponse<object>.Success(result);
        }

        public async Task<ApiResponse<CategoryDto>> CreateAsync(CategoryUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                return ApiResponse<CategoryDto>.Fail("Tên thể loại không được để trống.");

            if (await _repo.ExistsByNameAsync(dto.CategoryName))
                return ApiResponse<CategoryDto>.Fail("Tên thể loại này đã tồn tại.");

            var cat = new Category { CategoryName = dto.CategoryName.Trim() };
            _repo.Add(cat);

            return await _repo.SaveChangesAsync()
                ? ApiResponse<CategoryDto>.Success(new CategoryDto(cat.CategoryId, cat.CategoryName), "Tạo thể loại thành công.")
                : ApiResponse<CategoryDto>.Fail("Lỗi hệ thống khi tạo thể loại.", 500);
        }

        public async Task<ApiResponse<CategoryDto>> UpdateAsync(int id, CategoryUpsertDto dto)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null) return ApiResponse<CategoryDto>.Fail("Không tìm thấy thể loại.", 404);

            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                return ApiResponse<CategoryDto>.Fail("Tên thể loại không được để trống.");

            if (await _repo.ExistsByNameAsync(dto.CategoryName, id))
                return ApiResponse<CategoryDto>.Fail("Tên thể loại này đã tồn tại.");

            cat.CategoryName = dto.CategoryName.Trim();
            _repo.Update(cat);

            return await _repo.SaveChangesAsync()
                ? ApiResponse<CategoryDto>.Success(new CategoryDto(cat.CategoryId, cat.CategoryName), "Cập nhật thành công.")
                : ApiResponse<CategoryDto>.Fail("Lỗi hệ thống khi cập nhật.", 500);
        }

        public async Task<ApiResponse<object>> DeleteAsync(int id, bool force)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null) return ApiResponse<object>.Fail("Không tìm thấy thể loại.", 404);

            if (cat.Books != null && cat.Books.Any() && !force)
                return ApiResponse<object>.Fail($"Thể loại đang chứa {cat.Books.Count} sách. Hãy xóa sách trước hoặc dùng force=true.");

            _repo.Delete(cat);
            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(null, "Xóa thể loại thành công.")
                : ApiResponse<object>.Fail("Lỗi hệ thống khi xóa.", 500);
        }

        public async Task<ApiResponse<List<CategoryDto>>> SearchAsync(string keyword)
        {
            var cats = await _repo.SearchAsync(keyword);
            var data = cats.Select(c => new CategoryDto(c.CategoryId, c.CategoryName)).ToList();
            return ApiResponse<List<CategoryDto>>.Success(data);
        }
    }
}