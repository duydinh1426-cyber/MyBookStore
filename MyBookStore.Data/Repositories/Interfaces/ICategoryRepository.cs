using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int categoryId);
        Task<bool> Exists(int categoryId);
        Task<bool> ExistsByName(string categoryName, int? excludeId = null);

        Task AddAsync(Category category);
        Task DeleteAsync(Category category);
        Task UpdateAsync(Category category);

        Task<List<Category>> Search(string keyword);
        Task<(int total, List<Book> books)> GetBooksByCategory(int categoryId, int page, int pageSize);
        Task SaveChangesAsync();
        
    }
}
