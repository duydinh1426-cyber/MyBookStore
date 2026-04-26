using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(); // lấy toàn bộ thể loại
        Task<Category?> GetByIdAsync(int id); // lấy thể theo id
        Task<List<Category>> SearchAsync(string keyword); // tìm thể loại theo tên
        Task<bool> ExistsByIdAsync(int id); // check thể loại có tồn tại không
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null); // check tên thể loại có trùng kh
        void Add(Category category);
        void Update(Category category);
        void Delete(Category category);
        Task<bool> SaveChangesAsync();
    }
}