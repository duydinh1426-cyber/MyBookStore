using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly IBookService _service;

        public BookController(IBookService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetBooks([FromQuery]BookQueryDto queryDto)
        {
            var result = await _service.GetBooks(queryDto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("top-new")]
        public async Task<IActionResult> GetTopNew(int count = 6)
        {
            var result = await _service.GetTopNew(count);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("top-selling")]
        public async Task<IActionResult> GetTopSelling(int count = 6)
        {
            var result = await _service.GetTopSelling(count);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("top-rated")]
        public async Task<IActionResult> GetTopRated(int count = 6)
        {
            var result = await _service.GetTopRated(count);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetById(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(BookUpsertDto dto)
        {
            var result = await _service.Create(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, BookUpsertDto dto)
        {
            var result = await _service.Update(id, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.Delete(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
