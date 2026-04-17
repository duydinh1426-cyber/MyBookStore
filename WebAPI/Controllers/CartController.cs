using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/cart")]
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
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.TryParse(claim, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var result = await _service.GetCartAsync(GetUserId());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(AddCartDto dto)
        {
            var result = await _service.AddToCartAsync(GetUserId(), dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{bookId:int}")]
        public async Task<IActionResult> UpdateCart(int bookId, UpdateCartDto dto)
        {
            var result = await _service.UpdateCartAsync(GetUserId(), bookId, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{bookId:int}")]
        public async Task<IActionResult> RemoveFromCart(int bookId)
        {
            var result = await _service.RemoveFromCartAsync(GetUserId(), bookId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _service.ClearCartAsync(GetUserId());
            return StatusCode(result.StatusCode, result);
        }
    }
}