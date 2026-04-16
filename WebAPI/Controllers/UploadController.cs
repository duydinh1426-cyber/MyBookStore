using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Interfaces;

namespace BookStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IFileService _fileService;

        public UploadController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("image")]
        [Authorize(Roles = "Admin")] // Chỉ Admin mới được upload
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                var fileName = await _fileService.SaveImageAsync(file);
                var fileUrl = $"{Request.Scheme}://{Request.Host}/images/{fileName}";

                return Ok(new
                {
                    message = "Upload thành công",
                    url = fileUrl,
                    fileName = fileName
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "Lỗi server khi upload ảnh.");
            }
        }
    }
}