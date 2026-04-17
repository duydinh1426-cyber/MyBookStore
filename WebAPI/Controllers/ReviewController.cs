using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _service;
        public ReviewController(IReviewService service) => _service = service;

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.TryParse(claim, out var id) ? id : 0;
        }

        [HttpGet("book/{bookId:int}")]
        public async Task<IActionResult> GetByBook(int bookId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int? rating = null)
        {
            var res = await _service.GetByBookAsync(bookId, page, pageSize, rating);
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("status/{bookId:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetStatus(int bookId)
        {
            var res = await _service.GetReviewStatusAsync(GetUserId(), bookId);
            return StatusCode(res.StatusCode, res);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var res = await _service.CreateAsync(GetUserId(), dto);
            return StatusCode(res.StatusCode, res);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _service.DeleteAsync(id);
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? rating = null, [FromQuery] int? bookId = null)
        {
            var res = await _service.AdminGetAllAsync(page, pageSize, rating, bookId);
            return StatusCode(res.StatusCode, res);
        }
    }
}