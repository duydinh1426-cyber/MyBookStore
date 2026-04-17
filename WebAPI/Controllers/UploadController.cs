using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var result = await _fileService.SaveImageAsync(file);

            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, result);

            var fileName = result.Data;
            var fileUrl = $"{Request.Scheme}://{Request.Host}/images/{fileName}";

            var response = ApiResponse<object>.Success(
                new { fileName, url = fileUrl },
                "Upload thành công"
            );

            return StatusCode(response.StatusCode, response);
        }
    }
}