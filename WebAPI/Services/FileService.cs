using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            var contentPath = _env.WebRootPath;
            var path = Path.Combine(contentPath, "images");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                throw new ArgumentException("Chỉ chấp nhận file .jpg, .jpeg, .png, .webp");

            // Tạo tên file duy nhất
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fileNameWithPath = Path.Combine(path, fileName);

            using var stream = new FileStream(fileNameWithPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        public void DeleteImage(string fileName)
        {
            var path = Path.Combine(_env.WebRootPath, "images", fileName);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
