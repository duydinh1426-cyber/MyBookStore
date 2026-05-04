using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using WebAPI.DTOs;

namespace WebAPI.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;

        public CategoryService(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<ServiceResult<List<CategoryDto>>> GetAllAsync()
        {
            var cats = await _repo.GetAllAsync();

            var data = cats.Select(c => new CategoryDto(c.CategoryId, c.CategoryName)).ToList();
            return ServiceResult<List<CategoryDto>>.Success(data);
        }

        public async Task<ServiceResult<CategoryDto>> GetByIdAsync(int id)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null)
                return ServiceResult<CategoryDto>.Failure("Không tìm thấy thể loại.",404);

            var data = new CategoryDto(cat.CategoryId, cat.CategoryName);
            return ServiceResult<CategoryDto>.Success(data);
        }

        public async Task<ServiceResult<object>> GetBooksAsync(int id, int page, int pageSize)
        {
            if (!await _repo.ExistsByIdAsync(id))
                return ServiceResult<object>.Failure("Thể loại không tồn tại",404);

            var (total, books) = await _repo.GetBooksByCategoryAsync(id, page, pageSize);

            var data = new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                data = books.Select(b => new BookSummaryDto(
                    b.BookId, b.Title, b.Author, b.Price, b.Image,
                    null, b.NumberStock, b.NumberSold))
            };
            return ServiceResult<object>.Success(data);
        }

        public async Task<ServiceResult<CategoryDto>> CreateAsync(CategoryUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                return ServiceResult<CategoryDto>.Failure("Tên thể loại không được để trống.",400);

            if (await _repo.ExistsByNameAsync(dto.CategoryName))
                return ServiceResult<CategoryDto>.Failure("Thể loại đã tồn tại.", 409);

            var cat = new Category { CategoryName = dto.CategoryName.Trim() };
            _repo.Add(cat);

            var success = await _repo.SaveChangesAsync();
            if (!success)
                return ServiceResult<CategoryDto>.Failure("Lỗi hệ thống.", 500);

            var data = new CategoryDto(cat.CategoryId, cat.CategoryName);
            return ServiceResult<CategoryDto>.Success(data, "Thêm thể loại thành công.");
        }

        public async Task<ServiceResult<CategoryDto>> UpdateAsync(int id, CategoryUpsertDto dto)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null)
                return ServiceResult<CategoryDto>.Failure("Không tìm thấy thể loại.", 404);

            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                return ServiceResult<CategoryDto>.Failure("Tên thể loại không được để trống.",400);

            if (await _repo.ExistsByNameAsync(dto.CategoryName, id))
                return ServiceResult<CategoryDto>.Failure("Tên thể loại này đã tồn tại.", 409);

            cat.CategoryName = dto.CategoryName.Trim();
            _repo.Update(cat);

            var success = await _repo.SaveChangesAsync();

            if (!success)
                return ServiceResult<CategoryDto>.Failure("Lỗi hệ thống.", 500);

            var data = new CategoryDto(cat.CategoryId, cat.CategoryName);
            return ServiceResult<CategoryDto>.Success(data, "Cập nhật thể loại thành công.");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null)
                return ServiceResult.Failure("Không tìm thấy thể loại.",404);

            if (cat.Books != null && cat.Books.Any())
                return ServiceResult.Failure($"Thể loại còn {cat.Books.Count} cuốn sách. Không thể xóa!.",409);
            
            _repo.Delete(cat);
            if (!(await _repo.SaveChangesAsync()))
                return ServiceResult.Failure("Lỗi hệ thống.", 500);

            return ServiceResult.Success("Xóa thể loại thành công.");
        }

        public async Task<ServiceResult<List<CategoryDto>>> SearchAsync(string keyword)
        {
            var cats = await _repo.SearchAsync(keyword);
            var data = cats.Select(c => new CategoryDto(c.CategoryId, c.CategoryName)).ToList();
            return ServiceResult<List<CategoryDto>>.Success(data);
        }
    }
}