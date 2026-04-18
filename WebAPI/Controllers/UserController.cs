using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;
        public UsersController(IUserService service) => _service = service;

        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15)
        {
            return Ok(await _service.GetAllUsersAsync(keyword, page, pageSize));
        }

        [HttpGet("admin/{id:int}")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            var result = await _service.GetUserDetailAsync(id);

            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            return Ok(result);
        }

        [HttpPost("admin/{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var result = await _service.ResetPasswordAsync(id);
            dynamic res = result;

            if (res.message == "NotFound")
                return NotFound(new { message = "Không tìm thấy người dùng." });

            if (res.message != null)
                return StatusCode(500, new { message = "Lỗi khi cập nhật mật khẩu." });

            return Ok(result);
        }
    }
}