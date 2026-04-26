using Microsoft.AspNetCore.Http;

namespace WebAPI.Services.Helper
{
    public interface IFileService
    {
        Task<object> SaveImageAsync(IFormFile file);
        object DeleteImage(string fileName);
    }
}