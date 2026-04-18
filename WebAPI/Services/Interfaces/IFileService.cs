using Microsoft.AspNetCore.Http;

namespace WebAPI.Services.Interfaces
{
    public interface IFileService
    {
        Task<object> SaveImageAsync(IFormFile file);
        object DeleteImage(string fileName);
    }
}