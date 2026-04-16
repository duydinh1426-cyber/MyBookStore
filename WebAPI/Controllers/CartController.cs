using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;

        public CartController(ICartService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var result = await _service.GetCart(GetUserId());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(AddCartDto dto)
        {
            var result = await _service.AddToCart(GetUserId(), dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{bookId:int}")]
        public async Task<IActionResult> RemoveFromCart(int bookId)
        {
            var result = await _service.RemoveFromCart(GetUserId(), bookId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _service.ClearCart(GetUserId());
            return StatusCode(result.StatusCode, result);
        }
    }
}