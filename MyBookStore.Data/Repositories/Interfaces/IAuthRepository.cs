using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> IsEmailExistsAsync(string email); // kiểm tra email tồn tại chưa
        Task<Account?> GetByEmailAsync(string email); // lấy thông tin tài khoản theo email
        Task<Account?> GetByIdAsync(int accountId); // lấy thông tin tài khoản theo ID
        void AddAccount(Account account);
        void AddCustomer(Customer customer);
        void UpdateAccount(Account account);

        Task<bool> SaveChangesAsync(); // lưu thay đổi vào cơ sở dữ liệu
    }
}
