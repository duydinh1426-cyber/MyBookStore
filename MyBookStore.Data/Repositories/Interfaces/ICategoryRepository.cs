using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<List<Category>> SearchAsync(string keyword);
        Task<bool> ExistsByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<(int total, List<Book> books)> GetBooksByCategoryAsync(int categoryId, int page, int pageSize);
        void Add(Category category);
        void Update(Category category);
        void Delete(Category category);
        Task<bool> SaveChangesAsync();
    }
}