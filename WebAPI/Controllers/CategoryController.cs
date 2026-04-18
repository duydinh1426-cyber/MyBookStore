using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(bool includeBookCount = false)
        {
            var result = await _service.GetAllAsync(includeBookCount);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy thể loại." });
            return Ok(result);
        }

        [HttpGet("{id:int}/books")]
        public async Task<IActionResult> GetBooks(int id, int page = 1, int pageSize = 12)
        {
            var result = await _service.GetBooksAsync(id, page, pageSize);
            if (result == null) return NotFound(new { message = "Thể loại không tồn tại." });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CategoryUpsertDto dto)
        {
            var result = await _service.CreateAsync(dto);

            if (result is CategoryDto cat) return Ok(cat);
            return BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, CategoryUpsertDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            if (result is CategoryDto cat) return Ok(cat);

            dynamic res = result;
            string msg = res.message;

            if (res.message == "NotFound") return NotFound(new { message = "Không tìm thấy thể loại." });
            if (msg == "Thể loại đã tồn tại") return Conflict(res);
            return BadRequest(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, bool force = false)
        {
            var result = await _service.DeleteAsync(id, force);
            dynamic res = result;
            string msg = res.message;

            if (msg == "NotFound") return NotFound(new { message = "Không tìm thấy thể loại." });
            if (msg.Contains("cuốn sách")) return Conflict(res);

            return Ok(res);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string q = "")
        {
            var result = await _service.SearchAsync(q);
            return Ok(result);
        }
    }
}