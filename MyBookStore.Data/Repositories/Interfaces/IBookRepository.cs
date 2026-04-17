using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IBookRepository
    {
        IQueryable<Book> GetQuery();
        Task<Book?> GetByIdAsync(int id);
        void Add(Book book);
        void Update(Book book);
        void Delete(Book book);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task<bool> HasOrderItemsAsync(int bookId);
        Task<bool> SaveChangesAsync();
    }
}
