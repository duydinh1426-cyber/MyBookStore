using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Auth;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service) => _service = service;

        [HttpPost("register/send-otp")]
<<<<<<< HEAD
        public async Task<IActionResult> RegisterSendOtp(SendOtpDto dto)
=======
        public async Task<IActionResult> RegisterSendOtp(OtpRequest dto)
>>>>>>> f84b213 ( new chatbox)
        {
            var result = await _service.RegisterSendOtpAsync(dto);
            return HandleResult(result);
        }

        [HttpPost("register/verify-otp")]
        public async Task<IActionResult> RegisterVerifyOtp(VerifyRegisterOtpDto dto)
        {
            var result = await _service.RegisterVerifyOtpAsync(dto);
            return HandleResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _service.LoginAsync(dto);
            return HandleResult(result);
        }

        [HttpPost("forgot-password/send-otp")]
<<<<<<< HEAD
        public async Task<IActionResult> ForgotSendOtp(SendOtpDto dto)
=======
        public async Task<IActionResult> ForgotSendOtp(OtpRequest dto)
>>>>>>> f84b213 ( new chatbox)
        {
            var result = await _service.ForgotSendOtpAsync(dto);
            return HandleResult(result);
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> ForgotVerifyOtp(VerifyForgotOtpDto dto)
        {
            var result = await _service.ForgotVerifyOtpAsync(dto);
            return HandleResult(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var result = await _service.GetMeAsync(AccountId);
            return HandleResult(result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var result = await _service.UpdateMeAsync(AccountId, UserId, dto);
            return HandleResult(result);
        }

        [HttpPost("me/change-password/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeSendOtp(SendChangePasswordOtpDto dto)
        {
            var result = await _service.ChangeSendOtpAsync(AccountId, dto);
            return HandleResult(result);
        }

        [HttpPut("me/change-password/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeVerifyOtp(VerifyChangePasswordOtpDto dto)
        {
            var result = await _service.ChangeVerifyOtpAsync(AccountId, dto);
            return HandleResult(result);
        }

        [HttpPost("me/change-email/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeEmailSendOtp([FromBody] SendChangeEmailOtpDto dto)
        {
            var result = await _service.ChangeEmailSendOtpAsync(AccountId, dto.NewEmail);
            return HandleResult(result);
        }

        [HttpPut("me/change-email/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeEmailVerifyOtp([FromBody] VerifyChangeEmailOtpDto dto)
        {
            var result = await _service.ChangeEmailVerifyOtpAsync(AccountId, UserId, dto.NewEmail, dto.Otp);

            return HandleResult(result);
        }
    }
}