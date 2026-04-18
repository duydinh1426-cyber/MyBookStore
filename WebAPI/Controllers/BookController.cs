using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BookController : ControllerBase
    {
        private readonly IBookService _service;

        public BookController(IBookService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetBooks([FromQuery] BookQueryDto queryDto)
        {
            var result = await _service.GetBooksAsync(queryDto);
            return Ok(result);
        }

        [HttpGet("top-new")]
        public async Task<IActionResult> GetTopNew(int count = 6)
        {
            var result = await _service.GetTopNewAsync(count);
            return Ok(result);
        }

        [HttpGet("top-selling")]
        public async Task<IActionResult> GetTopSelling(int count = 6)
        {
            var result = await _service.GetTopSellingAsync(count);
            return Ok(result);
        }

        [HttpGet("top-rated")]
        public async Task<IActionResult> GetTopRated(int count = 6)
        {
            var result = await _service.GetTopRatedAsync(count);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy sách." });
            }
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(BookUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Tên sách không được để trống." });

            if (dto.Price <= 0)
                return BadRequest(new { message = "Giá sách phải lớn hơn 0." });

            var (message, bookId) = await _service.CreateAsync(dto);

            if (message != null)
            {
                return BadRequest(new { message = message });
            }

            return CreatedAtAction(nameof(GetById),
                new { id = bookId },
                new { message = "Thêm sách thành công.", bookId = bookId });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, BookUpsertDto dto)
        {
            var message = await _service.UpdateAsync(id, dto);
            if (message != null)
            {
                return BadRequest(new { message = message });
            }
            return Ok(new { message = "Cập nhật sách thành công." });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _service.DeleteAsync(id);
            if (message != null)
            {
                return BadRequest(new { message = message });
            }
            return Ok(new { message = "Xóa sách thành công." });
        }
    }
}