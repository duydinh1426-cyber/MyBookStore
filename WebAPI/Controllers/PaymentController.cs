using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentController(IPaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _configuration = configuration;
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.TryParse(claim, out var id) ? id : 0;
        }

        [HttpPost("vnpay/create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentDto dto)
        {
            var result = await _paymentService.CreateVnPayUrlAsync(GetUserId(), dto.OrderId, HttpContext);

            if (result.ContainsKey("message") && result["message"]?.ToString() == "NotFound")
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            if (result.ContainsKey("message") && result["message"]?.ToString() == "Forbidden")
                return Forbid();

            if (result.ContainsKey("message") && !string.IsNullOrEmpty(result["message"]?.ToString()))
                return BadRequest(new { message = result["message"] });

            return Ok(new { paymentUrl = result["paymentUrl"] });
        }

        [HttpGet("vnpay/callback")]
        public async Task<IActionResult> PaymentCallback()
        {
            var result = await _paymentService.HandleCallbackAsync(Request.Query);

            var success = result.TryGetValue("success", out var s) && s is true;
            var orderId = result.TryGetValue("orderId", out var o) ? o?.ToString() ?? "" : "";
            var code = result.TryGetValue("code", out var c) ? c?.ToString() ?? "" : "";

            var frontendUrl = _configuration["Frontend:PaymentResultUrl"];
            return Redirect($"{frontendUrl}?success={success.ToString().ToLower()}&orderId={orderId}&code={code}");
        }

        // Controllers/PaymentController.cs
        [HttpPost("qr/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> QrWebhook(
            [FromBody] SePayWebhookDto dto,
            [FromHeader(Name = "Authorization")] string? authHeader)
        {
            // Verify token từ SePay
            var token = _configuration["Payment:QR:SePayToken"];
            if (authHeader != $"Apikey {token}")
                return Unauthorized();

            // Chỉ xử lý tiền vào
            if (dto.TransferType != "in" || string.IsNullOrEmpty(dto.Content))
                return Ok(new { success = true });

            // Tìm mã đơn trong nội dung: "BS1042"
            var match = Regex.Match(dto.Content, @"BS(\d+)", RegexOptions.IgnoreCase);
            if (!match.Success) return Ok(new { success = true });

            var orderId = int.Parse(match.Groups[1].Value);
            var result = await _paymentService.ConfirmQrPaymentAsync(orderId, dto.TransferAmount);

            return Ok(new { success = true });
        }
    }
}