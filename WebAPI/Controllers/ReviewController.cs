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

        public ReviewController(IReviewService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            return int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "0"
            );
        }

        [HttpGet("book/{bookId:int}")]
        public async Task<IActionResult> GetByBook(
            int bookId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? rating = null)
        {
            var result = await _service.GetByBook(bookId, page, pageSize, rating);
            return Ok(result);
        }

        [HttpGet("status/{bookId:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetStatus(int bookId)
        {
            var userId = GetUserId();
            var result = await _service.GetReviewStatus(userId, bookId);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var userId = GetUserId();
            var result = await _service.Create(userId, dto);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.Delete(id);
            return Ok(result);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? rating = null,
            [FromQuery] int? bookId = null)
        {
            var result = await _service.AdminGetAll(page, pageSize, rating, bookId);
            return Ok(result);
        }
    }
}

