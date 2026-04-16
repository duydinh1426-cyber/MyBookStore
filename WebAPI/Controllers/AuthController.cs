using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        private int GetAccountId()
        {
            var value = User.FindFirstValue("accountId");
            return int.TryParse(value, out var accountId) ? accountId : 0;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? "0");
        }

        [HttpPost("register/send-otp")]
        public async Task<IActionResult> RegisterSendOtp(SendOtpDto dto)
        {
            var result = await _service.RegisterSendOtp(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("register/verify-otp")]
        public async Task<IActionResult> RegisterVerifyOtp(VerifyRegisterOtpDto dto)
        {
            var result = await _service.RegisterVerifyOtp(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _service.Login(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> ForgotSendOtp(SendOtpDto dto)
        {
            var result = await _service.ForgotSendOtp(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> ForgotVerifyOtp(VerifyForgotOtpDto dto)
        {
            var result = await _service.ForgotVerifyOtp(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var accountId = GetAccountId();
            var result = await _service.GetMe(accountId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var accountId = GetAccountId();
            var userId = GetUserId();

            var result = await _service.UpdateMe(accountId, userId, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("me/change-password/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeSendOtp(SendChangePasswordOtpDto dto)
        {
            var accountId = GetAccountId();
            var result = await _service.ChangeSendOtp(accountId, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("me/change-password/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeVerifyOtp(VerifyChangePasswordOtpDto dto)
        {
            var accountId = GetAccountId();
            var result = await _service.ChangeVerifyOtp(accountId, dto);
            return StatusCode(result.StatusCode, result);
        }
    }
}
