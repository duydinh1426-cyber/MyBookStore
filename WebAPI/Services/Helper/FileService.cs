using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WebAPI.Enums;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services.Helper
{ 
    public interface IFileService
    {
        Task<ServiceResult<string>> SaveImageAsync(IFormFile file);
        ServiceResult<string> DeleteImage(string fileName);
    }
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const string ImageFolder = "images";
        private const long MaxFileSize = 5 * 1024 * 1024;

        public FileService(IWebHostEnvironment env) => _env = env;

        public async Task<ServiceResult<string>> SaveImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ServiceResult<string>.Failure("Vui lòng chọn file ảnh.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                return ServiceResult<string>.Failure("Chỉ chấp nhận JPG, PNG, WEBP, GIF.");

            if (file.Length > MaxFileSize)
                return ServiceResult<string>.Failure("Ảnh không được vượt quá 5MB.");
            try
            {
                var uploadPath = Path.Combine(_env.WebRootPath, ImageFolder);
                Directory.CreateDirectory(uploadPath);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadPath, fileName);

                await using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                return ServiceResult<string>.Success(fileName);
            }
            catch (Exception)
            {
                return ServiceResult<string>.Failure("Lỗi hệ thống khi lưu tập tin.", 500);
            }
        }

        public ServiceResult<string> DeleteImage(string fileName)
        {
            try
            {
                var fullPath = Path.Combine(_env.WebRootPath, ImageFolder, fileName);

                if (!File.Exists(fullPath))
                    return ServiceResult<string>.Failure("Tập tin không tồn tại.", 404);

                File.Delete(fullPath);
                return ServiceResult<string>.Success(fileName, "Đã xóa ảnh thành công.");
            }
            catch (Exception)
            {
                return ServiceResult<string>.Failure("Lỗi khi xóa tập tin.", 500);
            }
        }
    }
}