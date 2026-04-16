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

        public AuthController(IAuthService service) => _service = service;

        #region Helper Methods
        private int GetAccountId()
        {
            var value = User.FindFirstValue("accountId");
            return int.TryParse(value, out var accountId) ? accountId : 0;
        }

        private int GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");
            return int.TryParse(value, out var userId) ? userId : 0;
        }
        #endregion

        [HttpPost("register/send-otp")]
        public async Task<IActionResult> RegisterSendOtp(SendOtpDto dto)
        {
            await _service.RegisterSendOtpAsync(dto);
            return Ok(new { message = "OTP đã được gửi đến email của bạn." });
        }

        [HttpPost("register/verify-otp")]
        public async Task<IActionResult> RegisterVerifyOtp(VerifyRegisterOtpDto dto)
        {
            await _service.RegisterVerifyOtpAsync(dto);
            return Ok(new { message = "Đăng ký tài khoản thành công." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _service.LoginAsync(dto);
            return Ok(result);
        }

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> ForgotSendOtp(SendOtpDto dto)
        {
            await _service.ForgotSendOtpAsync(dto);
            return Ok(new { message = "OTP khôi phục mật khẩu đã được gửi." });
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> ForgotVerifyOtp(VerifyForgotOtpDto dto)
        {
            await _service.ForgotVerifyOtpAsync(dto);
            return Ok(new { message = "Đổi mật khẩu thành công." });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var accountId = GetAccountId();
            var result = await _service.GetMeAsync(accountId);
            return Ok(result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var accountId = GetAccountId();
            var userId = GetUserId();
            var result = await _service.UpdateMeAsync(accountId, userId, dto);
            return Ok(result);
        }

        [HttpPost("me/change-password/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeSendOtp(SendChangePasswordOtpDto dto)
        {
            var accountId = GetAccountId();
            await _service.ChangeSendOtpAsync(accountId, dto);
            return Ok(new { message = "OTP xác nhận đổi mật khẩu đã được gửi." });
        }

        [HttpPut("me/change-password/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeVerifyOtp(VerifyChangePasswordOtpDto dto)
        {
            var accountId = GetAccountId();
            await _service.ChangeVerifyOtpAsync(accountId, dto);
            return Ok(new { message = "Mật khẩu đã được cập nhật thành công." });
        }
    }
}
