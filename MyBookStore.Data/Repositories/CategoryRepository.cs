using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DBContext _db;

        public CategoryRepository(DBContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Category category)
        {
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Category category)
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> Exists(int categoryId)
        {
            return await _db.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task<bool> ExistsByName(string categoryName, int? excludeId = null)
        {
            return await _db.Categories.AnyAsync(c =>
                c.CategoryName.ToLower() == categoryName.ToLower().Trim() &&
                (!excludeId.HasValue || c.CategoryId != excludeId));
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _db.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<(int total, List<Book> books)> GetBooksByCategory(int categoryId, int page, int pageSize)
        {
            var query = _db.Books
                .Where(b => b.CategoryId == categoryId)
                .OrderBy(b => b.Title);

            var total = await query.CountAsync();

            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (total, books);
        }

        public async Task<Category?> GetCategoryByIdAsync(int categoryId)
        {
            return await _db.Categories.FindAsync(categoryId);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<List<Category>> Search(string keyword)
        {
            return await _db.Categories
                .Where(c => c.CategoryName.Contains(keyword))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
        }
    }
}
