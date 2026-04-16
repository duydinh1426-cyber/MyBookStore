using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrderController(IOrderService service)
        {
            _service = service;
        }

        private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var result = await _service.Checkout(GetUserId(), dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetById(GetUserId(), User.IsInRole("Admin"), id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.Cancel(GetUserId(), id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("admin/{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var result = await _service.UpdateStatus(id, dto);
            return StatusCode(result.StatusCode, result);
        }
    }
}
