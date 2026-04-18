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

        [HttpPost("checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Checkout(CheckoutDto dto)
        {
            var result = await _service.CheckoutAsync(GetUserId(), dto);
            dynamic res = result;

            if (res.message == "Giỏ hàng của bạn đang trống." ||
                res.message == "Lỗi hệ thống khi xử lý đơn hàng.")
                return BadRequest(result);

            string msg = res.message;
            if (msg != null && msg.Contains("chỉ còn"))
                return BadRequest(result);

            return StatusCode(201, result);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            return Ok(await _service.GetUserOrdersAsync(GetUserId(), page, pageSize, status));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(GetUserId(), User.IsInRole("Admin"), id);
            if (result == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            dynamic res = result;
            if (res.message == "Forbidden") return Forbid();

            return Ok(result);
        }

        [HttpPut("{id:int}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.CancelAsync(GetUserId(), id);
            dynamic res = result;

            if (res.message == "NotFound") return NotFound(new { message = "Không tìm thấy đơn hàng." });
            if (res.message == "Forbidden") return Forbid();

            string msg = res.message;
            if (msg != null && (msg.StartsWith("Không thể") || msg.StartsWith("Lỗi hệ thống")))
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAll([FromQuery] string? status, [FromQuery] string? keyword)
        {
            return Ok(await _service.AdminGetAllOrdersAsync(status, keyword));
        }

        [HttpPut("admin/{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
        {
            var result = await _service.UpdateStatusAsync(id, dto);
            dynamic res = result;

            if (res.message == "NotFound") return NotFound(new { message = "Không tìm thấy đơn hàng." });
            if (res.success == false) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("admin/stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _service.GetAdminStatsAsync(from, to));
        }
    }
}