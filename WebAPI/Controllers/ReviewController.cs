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
        public async Task<IActionResult> GetByBook(int bookId, int page = 1, int pageSize = 10, int? rating = null)
        {
            var result = await _service.GetByBookAsync(bookId, page, pageSize, rating);
            dynamic res = result;
            if (res.message == "NotFound") return NotFound(new { message = "Sách không tồn tại." });
            return Ok(result);
        }

        [HttpGet("status/{bookId:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetStatus(int bookId)
        {
            return Ok(await _service.GetReviewStatusAsync(GetUserId(), bookId));
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(CreateReviewDto dto)
        {
            var result = await _service.CreateAsync(GetUserId(), dto);
            dynamic res = result;

            if (res.message == "NotFound") return NotFound(new { message = "Sách không tồn tại." });
            if (res.message == "Đánh giá phải từ 1 đến 5 sao." ||
                res.message == "Bạn cần mua sản phẩm này trước khi đánh giá." ||
                res.message == "Bạn đã đánh giá sản phẩm này rồi." ||
                res.message == "Lỗi khi lưu đánh giá.")
                return BadRequest(result);

            return StatusCode(201, result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            dynamic res = result;

            if (res.message == "NotFound") return NotFound(new { message = "Không tìm thấy đánh giá." });
            if (res.message == "Lỗi khi xóa đánh giá.") return StatusCode(500, result);

            return Ok(result);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAll(int page = 1, int pageSize = 20, int? rating = null, int? bookId = null)
        {
            return Ok(await _service.AdminGetAllAsync(page, pageSize, rating, bookId));
        }
    }
}