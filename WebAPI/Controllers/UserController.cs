using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services.User;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : BaseController
    {
        private readonly IUserService _service;
        public UsersController(IUserService service) => _service = service;

        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _service.GetAllUsersAsync(keyword, page, pageSize);
            return HandleResult(result);
        }

        [HttpGet("admin/{id:int}")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            var result = await _service.GetUserDetailAsync(id);
            return HandleResult(result);
        }
    }
}