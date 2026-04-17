using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;

namespace Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DBContext _db;
        public CategoryRepository(DBContext db) => _db = db;

        public async Task<List<Category>> GetAllAsync()
        {
            return await _db.Categories
                .Include(c => c.Books)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _db.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        public async Task<List<Category>> SearchAsync(string keyword)
        {
            return await _db.Categories
                .Where(c => EF.Functions.Like(c.CategoryName, $"%{keyword}%"))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<bool> ExistsByIdAsync(int id) =>
            await _db.Categories.AnyAsync(c => c.CategoryId == id);

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var cleanName = name.Trim().ToLower();
            return await _db.Categories.AnyAsync(c =>
                c.CategoryName.ToLower() == cleanName && (!excludeId.HasValue || c.CategoryId != excludeId));
        }

        public async Task<(int total, List<Book> books)> GetBooksByCategoryAsync(int categoryId, int page, int pageSize)
        {
            var query = _db.Books
                .Where(b => b.CategoryId == categoryId)
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (total, books);
        }

        public void Add(Category category) => _db.Categories.Add(category);
        public void Update(Category category) => _db.Categories.Update(category);
        public void Delete(Category category) => _db.Categories.Remove(category);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}