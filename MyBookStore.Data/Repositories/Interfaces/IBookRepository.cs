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
        Task AddAsync(Book book);
        Task UpdateAsync(Book book);
        Task DeleteAsync(Book book);
        Task<bool> CategoryExists(int categoryId);
        Task<bool> HasOrderItems(int bookId);
        Task SaveChangesAsync();
    }
}
