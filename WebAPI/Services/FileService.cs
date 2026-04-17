using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const string ImageFolder = "images";

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<ApiResponse<string>> SaveImageAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return ApiResponse<string>.Fail("File không hợp lệ hoặc trống.");

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(ext))
                    return ApiResponse<string>.Fail("Định dạng file không hỗ trợ (Chỉ nhận .jpg, .jpeg, .png, .webp).");

                // Tạo đường dẫn thư mục images trong wwwroot
                var contentPath = _env.WebRootPath;
                var path = Path.Combine(contentPath, ImageFolder);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                // Tạo tên file duy nhất để tránh trùng lặp
                var fileName = $"{Guid.NewGuid()}{ext}";
                var fileNameWithPath = Path.Combine(path, fileName);

                using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return ApiResponse<string>.Success(fileName, "Lưu ảnh thành công.");
            }
            catch (Exception)
            {
                return ApiResponse<string>.Fail("Lỗi hệ thống khi lưu tập tin.", 500);
            }
        }

        public ApiResponse<object> DeleteImage(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return ApiResponse<object>.Fail("Tên file không hợp lệ.");

                var path = Path.Combine(_env.WebRootPath, ImageFolder, fileName);

                if (File.Exists(path))
                {
                    File.Delete(path);
                    return ApiResponse<object>.Success(null, "Đã xóa ảnh thành công.");
                }

                return ApiResponse<object>.Fail("Tập tin không tồn tại trên hệ thống.", 404);
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail("Lỗi khi xóa tập tin.", 500);
            }
        }
    }
}