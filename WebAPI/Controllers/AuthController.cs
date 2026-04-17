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
            var result = await _service.RegisterSendOtpAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("register/verify-otp")]
        public async Task<IActionResult> RegisterVerifyOtp(VerifyRegisterOtpDto dto)
        {
            var result = await _service.RegisterVerifyOtpAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _service.LoginAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> ForgotSendOtp(SendOtpDto dto)
        {
            var result = await _service.ForgotSendOtpAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> ForgotVerifyOtp(VerifyForgotOtpDto dto)
        {
            var result = await _service.ForgotVerifyOtpAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var result = await _service.GetMeAsync(GetAccountId());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var result = await _service.UpdateMeAsync(GetAccountId(), GetUserId(), dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("me/change-password/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeSendOtp(SendChangePasswordOtpDto dto)
        {
            var result = await _service.ChangeSendOtpAsync(GetAccountId(), dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("me/change-password/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeVerifyOtp(VerifyChangePasswordOtpDto dto)
        {
            var result = await _service.ChangeVerifyOtpAsync(GetAccountId(), dto);
            return StatusCode(result.StatusCode, result);
        }
    }
}
