using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [Route("api/cart")]
    [Authorize]
    public class CartController : BaseController
    {
        private readonly ICartService _service;

        public CartController(ICartService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var result = await _service.GetCartAsync(UserId);
            return HandleResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(AddCartDto dto)
        {
            var result = await _service.AddToCartAsync(UserId, dto);
            return HandleResult(result);
        }

        [HttpPut("{bookId:int}")]
        public async Task<IActionResult> UpdateCart(int bookId, UpdateCartDto dto)
        {
            var result = await _service.UpdateCartAsync(UserId, bookId, dto);
            return HandleResult(result);
        }

        [HttpDelete("{bookId:int}")]
        public async Task<IActionResult> RemoveFromCart(int bookId)
        {
            var result = await _service.RemoveFromCartAsync(UserId, bookId);
            return HandleResult(result);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _service.ClearCartAsync(UserId);
            return HandleResult(result);
        }
    }
}