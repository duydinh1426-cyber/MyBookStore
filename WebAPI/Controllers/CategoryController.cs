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
        public CategoryController(ICategoryService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeBookCount = false)
        {
            var res = await _service.GetAllAsync(includeBookCount);
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _service.GetByIdAsync(id);
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("{id:int}/books")]
        public async Task<IActionResult> GetBooks(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var res = await _service.GetBooksAsync(id, page, pageSize);
            return StatusCode(res.StatusCode, res);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CategoryUpsertDto dto)
        {
            var res = await _service.CreateAsync(dto);
            return StatusCode(res.StatusCode, res);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, CategoryUpsertDto dto)
        {
            var res = await _service.UpdateAsync(id, dto);
            return StatusCode(res.StatusCode, res);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, [FromQuery] bool force = false)
        {
            var res = await _service.DeleteAsync(id, force);
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q = "")
        {
            var res = await _service.SearchAsync(q);
            return StatusCode(res.StatusCode, res);
        }
    }
}