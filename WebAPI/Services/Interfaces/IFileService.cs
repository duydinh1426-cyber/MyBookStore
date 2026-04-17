using Microsoft.AspNetCore.Http;
using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IFileService
    {
        Task<ApiResponse<string>> SaveImageAsync(IFormFile file);
        ApiResponse<object> DeleteImage(string fileName);
    }
}