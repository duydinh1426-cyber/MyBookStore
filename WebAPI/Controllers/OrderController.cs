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
        public OrderController(IOrderService service) => _service = service;

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.TryParse(claim, out var id) ? id : 0;
        }

        // --- DÀNH CHO CUSTOMER ---

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var res = await _service.CheckoutAsync(GetUserId(), dto);
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var res = await _service.GetUserOrdersAsync(GetUserId());
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _service.GetByIdAsync(GetUserId(), User.IsInRole("Admin"), id);
            return StatusCode(res.StatusCode, res);
        }

        [HttpPut("{id:int}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var res = await _service.CancelAsync(GetUserId(), id);
            return StatusCode(res.StatusCode, res);
        }

        // --- DÀNH CHO ADMIN ---

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAll([FromQuery] string? status, [FromQuery] string? q)
        {
            var res = await _service.AdminGetAllOrdersAsync(status, q);
            return StatusCode(res.StatusCode, res);
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var res = await _service.UpdateStatusAsync(id, dto);
            return StatusCode(res.StatusCode, res);
        }

        [HttpGet("admin/stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminStats()
        {
            var res = await _service.GetAdminStatsAsync();
            return StatusCode(res.StatusCode, res);
        }
    }
}