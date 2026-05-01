using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Orders;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController : BaseController
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
            return HandleResult(result);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var result = await _service.GetUserOrdersAsync(GetUserId(), page, pageSize, status);
            return HandleResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(GetUserId(), User.IsInRole("Admin"), id);
            return HandleResult(result);
        }


        [HttpPut("{id:int}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderDto? dto = null)
        {
            var result = await _service.CancelAsync(GetUserId(), id, dto);
            return HandleResult(result);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAll(
            [FromQuery] string? status,
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _service.AdminGetAllOrdersAsync(status, keyword, page, pageSize);
            return HandleResult(result);
        }

        [HttpPut("admin/{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
        {
            var result = await _service.UpdateStatusAsync(id, dto);
            return HandleResult(result);
        }

        [HttpGet("admin/stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _service.GetAdminStatsAsync(from, to);
            return HandleResult(result);
        }

        [HttpGet("admin/refunds")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRefunds([FromQuery] string? status)
        {
            var result = await _service.GetRefundRequestsAsync(status);
            return HandleResult(result);
        }
            

        [HttpPut("admin/refunds/{refundId:int}/resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResolveRefund(int refundId, ResolveRefundDto dto)
        {
            var result = await _service.ResolveRefundAsync(refundId, dto.AdminNote);
            return HandleResult(result);
        }

        // Kiểm tra đơn đã được thanh toán chưa
        [HttpGet("qr/status/{orderId:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> QrStatus(int orderId)
        {
            var result = await _service.GetOrderByIdAsync(orderId, GetUserId());
            return HandleResult(result);
        }
    }
}