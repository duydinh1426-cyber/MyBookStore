using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IBookRepository
    {
        // filter, sort, paging,...
        Task<(int total, int page, int pageSize, int totalPages, List<Book> data)> GetBookAsync( 
            int page = 1,
            int pageSize = 10,
            string? keyword = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string sortBy = "createdAt",
            string sortOrder = "desc");

        Task<List<Book>> GetTopNewAsync(int count); // lấy top sách mới
        Task<List<Book>> GetTopSellingAsync(int count); // lấy top sách bán chạy
        Task<List<Book>> GetTopRatedAsync(int count); // lấy top sách đánh giá cao nhất
        Task<Book?> GetByIdAsync(int id); // lấy sách theo id, trả về null nếu kh tìm thấy
        Task<bool> HasOrderItemsAsync(int bookId); // kiểm tra tồn tại trong đơn hàng nào chưa
        void Add(Book book);
        void Update(Book book);
        void Delete(Book book);
        Task<bool> SaveChangesAsync();
    }
}
