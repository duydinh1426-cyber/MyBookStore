using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Interfaces;
using WebAPI.DTOs;
using WebAPI.Services.Helper;

namespace WebAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class HelperController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IEmailService _emailService;

        public HelperController(IFileService fileService, IEmailService emailService)
        {
            _fileService = fileService;
            _emailService = emailService;
        }

        // ================== UPLOAD ==================
        [HttpPost("upload/image")]
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

        [HttpDelete("upload/image/{fileName}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteImage(string fileName)
        {
            var result = _fileService.DeleteImage(fileName);
            dynamic res = result;

            if (res.message == "NotFound")
                return NotFound(new { message = "Tập tin không tồn tại." });

            return Ok(new { message = "Đã xóa ảnh thành công." });
        }

        // ================== CONTACT ==================
        [HttpPost("contact/send")]
        [AllowAnonymous]
        public async Task<IActionResult> Send([FromBody] ContactDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Message))
            {
                return BadRequest(new { message = "Email và nội dung không được để trống." });
            }

            await _emailService.SendContactAsync(dto.Name ?? "Khách hàng", dto.Email, dto.Message);

            return Ok(new { message = "Gửi thành công" });
        }
    }
}