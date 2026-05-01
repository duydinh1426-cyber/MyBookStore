using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Interfaces;
using WebAPI.DTOs;
using WebAPI.Services.Helper;

namespace WebAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class HelperController : BaseController
    {
        private readonly IFileService _fileService;
        private readonly IEmailService _emailService;

        public HelperController(IFileService fileService, IEmailService emailService)
        {
            _fileService = fileService;
            _emailService = emailService;
        }

        [HttpPost("upload/image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var result = await _fileService.SaveImageAsync(file);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(new
            {
                fileName = result.Data,
                url = $"/images/{result.Data}"
            });
        }

        [HttpDelete("upload/image/{fileName}")]
        public IActionResult DeleteImage(string fileName)
        {
            var result = _fileService.DeleteImage(fileName);
            return HandleResult(result);
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