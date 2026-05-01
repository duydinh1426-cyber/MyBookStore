using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [Route("api/books")]
    public class BookController : BaseController
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
            return HandleResult(result);
        }

        [HttpGet("top-new")]
        public async Task<IActionResult> GetTopNew(int count = 6)
        {
            var result = await _service.GetTopNewAsync(count);
            return HandleResult(result);
        }

        [HttpGet("top-selling")]
        public async Task<IActionResult> GetTopSelling(int count = 6)
        {
            var result = await _service.GetTopSellingAsync(count);
            return HandleResult(result);
        }

        [HttpGet("top-rated")]
        public async Task<IActionResult> GetTopRated(int count = 6)
        {
            var result = await _service.GetTopRatedAsync(count);
            return HandleResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(BookUpsertDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return HandleResult(result);

        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, BookUpsertDto dto)
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
    }
}