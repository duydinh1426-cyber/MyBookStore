using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> EmailExists(string email); // kiểm tra email tồn tại chưa
        Task<bool> UserNameExists(string username); // kiểm tra username tồn tại chưa

        Task<Account?> GetByUsername(string username); // lấy thông tin tài khoản theo username
        Task<Account?> GetByEmail(string email); // lấy thông tin tài khoản theo email
        Task<Account?> GetById(int accountId); // lấy thông tin tài khoản theo ID

        Task<Account> CreateAccount(Account account); // tạo tài khoản mới 
        Task CreateCustomer(Customer customer); // tạo thông tin khách hàng mới sau khi tạo tài khoản

        Task UpdateAccount(Account account); // cập nhật thông tin tài khoản
        Task SaveChanges(); // lưu thay đổi vào cơ sở dữ liệu
    }
}
