using Microsoft.AspNetCore.Http;

namespace WebAPI.Services.Interfaces
{
    public interface IFileService
    {
        Task<ServiceResult<string>> SaveImageAsync(IFormFile file);
        ServiceResult<string> DeleteImage(string fileName);
    }
}