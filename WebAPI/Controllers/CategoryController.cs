using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Services.Categories;

namespace WebAPI.Controllers
{
    [Route("api/categories")]
    public class CategoryController : BaseController
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return HandleResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return HandleResult(result);
        }

        [HttpGet("{id:int}/books")]
        public async Task<IActionResult> GetBooks(int id, int page = 1, int pageSize = 12)
        {
            var result = await _service.GetBooksAsync(id, page, pageSize);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CategoryUpsertDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return HandleResult(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, CategoryUpsertDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return HandleResult(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return HandleResult(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string q = "")
        {
            var result = await _service.SearchAsync(q);
            return HandleResult(result);
        }
    }
}