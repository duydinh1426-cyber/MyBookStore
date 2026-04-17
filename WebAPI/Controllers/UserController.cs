using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var res = await _service.GetAllUsersAsync(keyword, page, pageSize);
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("admin/{id:int}")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            var res = await _service.GetUserDetailAsync(id);
            return StatusCode(res.StatusCode, res);
        }

        [HttpPost("admin/{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var res = await _service.ResetPasswordAsync(id);
            return StatusCode(res.StatusCode, res);
        }
    }
}