using Data.Models;
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
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                return await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.CategoryName.Contains(keyword))
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
            }
            return new List<Category>();
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            return await _db.Categories.AnyAsync(c => c.CategoryId == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var cleanName = name.Trim();

            return await _db.Categories.AnyAsync(c =>
                c.CategoryName == cleanName && 
                (!excludeId.HasValue || c.CategoryId != excludeId)); // kiểm tra có phải là chính nó kh
        }

        public void Add(Category category) => _db.Categories.Add(category);
        public void Update(Category category) => _db.Categories.Update(category);
        public void Delete(Category category) => _db.Categories.Remove(category);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}