using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [Route("api/upload")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IFileService _fileService;

        public UploadController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var result = await _fileService.SaveImageAsync(file);
            dynamic res = result;

            if (res.error != null && res.error == true)
            {
                return BadRequest(new { message = res.message });
            }


            return Ok(new
            {
                message = "Upload thành công.",
                fileName = res.fileName,
                url = $"/images/{res.fileName}"
            });
        }

        // Endpoint xóa ảnh (nếu cần dùng cho Admin)
        [HttpDelete("image/{fileName}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteImage(string fileName)
        {
            var result = _fileService.DeleteImage(fileName);
            dynamic res = result;

            if (res.message == "NotFound")
                return NotFound(new { message = "Tập tin không tồn tại trên hệ thống." });

            return Ok(new { message = "Đã xóa ảnh thành công." });
        }
    }
}