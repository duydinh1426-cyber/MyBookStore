using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [Route("api/reviews")]
    public class ReviewController : BaseController
    {
        private readonly IReviewService _service;
        public ReviewController(IReviewService service) => _service = service;

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyReviews(int page = 1, int pageSize = 10)
        {
            var result = await _service.GetMyReviewsAsync(UserId, page, pageSize);
            return HandleResult(result);
        }

        [HttpGet("book/{bookId:int}")]
        public async Task<IActionResult> GetByBook(int bookId, int page = 1, int pageSize = 10, int? rating = null)
        {
            var result = await _service.GetByBookAsync(bookId, page, pageSize, rating);
            return HandleResult(result);
        }

        [HttpGet("status/{bookId:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetStatus(int bookId)
        {
            var result = await _service.GetReviewStatusAsync(UserId, bookId);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(CreateReviewDto dto)
        {
            var result = await _service.CreateAsync(UserId, dto);
            return HandleResult(result);
        }

        // Customer tự xóa đánh giá của mình
        [HttpDelete("my/{id:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteMine(int id)
        {
            var result = await _service.DeleteMyAsync(UserId, id);
            return HandleResult(result);
        }

        // Admin xóa bất kỳ đánh giá nào
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return HandleResult(result);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAll(int page = 1, int pageSize = 20, int? rating = null, int? bookId = null)
        {
            var result = await _service.AdminGetAllAsync(page, pageSize, rating, bookId);
            return HandleResult(result);
        }
    }
}