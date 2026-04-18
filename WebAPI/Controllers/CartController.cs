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
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(AddCartDto dto)
        {
            var message = await _service.AddToCartAsync(GetUserId(), dto);
            if (message != null)
            {
                return BadRequest(new { message = message });
            }
            return Ok(new { message = "Đã thêm vào giỏ hàng." });
        }

        [HttpPut("{bookId:int}")]
        public async Task<IActionResult> UpdateCart(int bookId, UpdateCartDto dto)
        {
            var message = await _service.UpdateCartAsync(GetUserId(), bookId, dto);
            if (message != null)
            {
                return BadRequest(new { message = message });
            }

            if (dto.Quantity <= 0)
                return Ok(new { message = "Đã xóa sách khỏi giỏ hàng." });

            return Ok(new { message = "Cập nhật giỏ hàng thành công." });
        }

        [HttpDelete("{bookId:int}")]
        public async Task<IActionResult> RemoveFromCart(int bookId)
        {
            var message = await _service.RemoveFromCartAsync(GetUserId(), bookId);
            if (message != null)
            {
                return NotFound(new { message = message });
            }
            return Ok(new { message = "Đã xóa sách khỏi giỏ hàng." });
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var message = await _service.ClearCartAsync(GetUserId());
            if (message != null)
            {
                return BadRequest(new { message = message });
            }
            return Ok(new { message = "Đã xóa toàn bộ giỏ hàng." });
        }
    }
}