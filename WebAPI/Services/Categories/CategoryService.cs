using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using WebAPI.DTOs;

namespace WebAPI.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly IBookRepository _bookRepository;

        public CategoryService(ICategoryRepository repo, IBookRepository bookRepository)
        {
            _repo = repo;
            _bookRepository = bookRepository;
        }

        public async Task<object> GetAllAsync(bool includeBookCount)
        {
            var cats = await _repo.GetAllAsync();

            if (includeBookCount)
            {
                return cats.Select(c => new {
                    categoryId = c.CategoryId,
                    categoryName = c.CategoryName,
                    bookCount = c.Books?.Count ?? 0
                }).ToList();
            }

            return cats.Select(c => new CategoryDto(c.CategoryId, c.CategoryName)).ToList();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null) return null;

            return new CategoryDto(cat.CategoryId, cat.CategoryName);
        }

        public async Task<object?> GetBooksAsync(int id, int page, int pageSize)
        {
            if (!await _repo.ExistsByIdAsync(id))
                return null;

            var result = await _bookRepository.GetBookAsync(
                page: page,
                pageSize: pageSize,
                categoryId: id
            );
            return new
            {
                total = result.total,
                page = result.page,
                pageSize = result.pageSize,
                totalPages = result.totalPages,
                data = result.data.Select(b => new BookSummaryDto(
                    b.BookId,
                    b.Title,
                    b.Author,
                    b.Price,
                    b.Image,
                    b.Category?.CategoryName, 
                    b.NumberStock,
                    b.NumberSold))
            };
        }

        public async Task<object> CreateAsync(CategoryUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                return new { message = "Tên thể loại không được để trống." };

            if (await _repo.ExistsByNameAsync(dto.CategoryName))
                return new { message = "Thể loại đã tồn tại." };

            var cat = new Category { CategoryName = dto.CategoryName.Trim() };
            _repo.Add(cat);

            if (await _repo.SaveChangesAsync())
                return new CategoryDto(cat.CategoryId, cat.CategoryName);

            return new CategoryDto(cat.CategoryId, cat.CategoryName);
        }

        public async Task<object> UpdateAsync(int id, CategoryUpsertDto dto)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null) return new { message = "NotFound" };

            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                return new { message = "Tên thể loại không được để trống." };

            if (await _repo.ExistsByNameAsync(dto.CategoryName, id))
                return new { message = "Tên thể loại này đã tồn tại." };

            cat.CategoryName = dto.CategoryName.Trim();
            _repo.Update(cat);

            if (await _repo.SaveChangesAsync())
                return new CategoryDto(cat.CategoryId, cat.CategoryName);

            return new { message = "Lỗi hệ thống khi cập nhật." };
        }

        public async Task<object> DeleteAsync(int id, bool force)
        {
            var cat = await _repo.GetByIdAsync(id);
            if (cat == null) return new { message = "NotFound" };

            if (cat.Books != null && cat.Books.Any() && !force)
            {
                return new
                {
                    message = $"Thể loại còn {cat.Books.Count} cuốn sách. Dùng ?force=true để xóa bắt buộc",
                    bookCount = cat.Books.Count
                };
            }

            _repo.Delete(cat);
            if (await _repo.SaveChangesAsync())
                return new { message = "Xóa thể loại thành công." };

            return new { message = "Đã xóa thể loại thành công" };
        }

        public async Task<List<CategoryDto>> SearchAsync(string keyword)
        {
            var cats = await _repo.SearchAsync(keyword);
            return cats.Select(c => new CategoryDto(c.CategoryId, c.CategoryName)).ToList();
        }
    }
}