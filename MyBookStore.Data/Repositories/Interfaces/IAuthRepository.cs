using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> IsEmailExistsAsync(string email); // kiểm tra email tồn tại chưa
        Task<bool> IsUsernameExistsAsync(string username); // kiểm tra username tồn tại chưa

        Task<Account?> GetByUsernameAsync(string username); // lấy thông tin tài khoản theo username
        Task<Account?> GetByEmailAsync(string email); // lấy thông tin tài khoản theo email
        Task<Account?> GetByIdAsync(int accountId); // lấy thông tin tài khoản theo ID

        void AddAccount(Account account);
        void AddCustomer(Customer customer);
        void UpdateAccount(Account account);

        Task<bool> SaveChangesAsync(); // lưu thay đổi vào cơ sở dữ liệu
    }
}
