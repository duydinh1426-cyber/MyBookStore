using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const string ImageFolder = "images";

        public FileService(IWebHostEnvironment env) => _env = env;

        public async Task<object> SaveImageAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return new { error = true, message = "Vui lòng chọn file ảnh." };

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(ext))
                    return new { error = true, message = "Chỉ chấp nhận JPG, PNG, WEBP, GIF." };

                // Giới hạn 5MB
                if (file.Length > 5 * 1024 * 1024)
                    return new { error = true, message = "Ảnh không được vượt quá 5MB." };

                var contentPath = _env.WebRootPath;
                var path = Path.Combine(contentPath, ImageFolder);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fileNameWithPath = Path.Combine(path, fileName);

                using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return new { error = false, fileName };
            }
            catch (Exception)
            {
                return new { error = true, message = "Lỗi hệ thống khi lưu tập tin." };
            }
        }

        public object DeleteImage(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return new { message = "Tên file không hợp lệ." };

                var path = Path.Combine(_env.WebRootPath, ImageFolder, fileName);

                if (File.Exists(path))
                {
                    File.Delete(path);
                    return new { message = "Đã xóa ảnh thành công." };
                }

                return new { message = "NotFound" };
            }
            catch (Exception ex)
            {
                return new { message = "Lỗi khi xóa tập tin.", detail = ex.Message };
            }
        }
    }
}