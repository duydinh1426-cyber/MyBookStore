using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
            var result = await _service.GetAll(includeBookCount);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetById(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}/book")]
        public async Task<IActionResult> GetBooks(int id, int page = 1, int pageSize = 12)
        {
            var result = await _service.GetBooks(id, page, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CategoryUpsertDto dto)
        {
            var result = await _service.Create(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, CategoryUpsertDto dto)
        {
            var result = await _service.Update(id, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, bool force = false)
        {
            var result = await _service.Delete(id, force);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string q = "")
        {
            var res = await _service.Search(q);
            return StatusCode(res.StatusCode, res);
        }
    }
}
