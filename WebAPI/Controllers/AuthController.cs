using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services.Auth;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service) => _service = service;

        private int GetAccountId()
        {
            var value = User.FindFirstValue("accountId");
            return int.TryParse(value, out var accountId) ? accountId : 0;
        }

        private int GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.TryParse(value, out var userId) ? userId : 0;
        }

        [HttpPost("register/send-otp")]
        public async Task<IActionResult> RegisterSendOtp(SendOtpDto dto)
        {
            var message = await _service.RegisterSendOtpAsync(dto);
            if (message != null) return BadRequest(new { message = message });
            return Ok(new { message = "Mã OTP đã được gửi đến email." });
        }

        [HttpPost("register/verify-otp")]
        public async Task<IActionResult> RegisterVerifyOtp(VerifyRegisterOtpDto dto)
        {
            var message = await _service.RegisterVerifyOtpAsync(dto);
            if (message != null) return BadRequest(new { message = message });
            return Ok(new { message = "Đăng ký tài khoản thành công." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _service.LoginAsync(dto);
            if (result == null) return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });
            return Ok(result);
        }

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> ForgotSendOtp(SendOtpDto dto)
        {
            var message = await _service.ForgotSendOtpAsync(dto);
            if (message != null) return BadRequest(new { message = message });
            return Ok(new { message = "Mã khôi phục đã được gửi." });
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> ForgotVerifyOtp(VerifyForgotOtpDto dto)
        {
            var message = await _service.ForgotVerifyOtpAsync(dto);
            if (message != null) return BadRequest(new { message = message });
            return Ok(new { message = "Đổi mật khẩu thành công." });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var result = await _service.GetMeAsync(GetAccountId());
            if (result == null) return NotFound(new { message = "Không tìm thấy tài khoản." });
            return Ok(result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var result = await _service.UpdateMeAsync(GetAccountId(), GetUserId(), dto);
            if (result == null) return BadRequest(new { message = "Cập nhật thông tin thất bại." });
            return Ok(result);
        }

        [HttpPost("me/change-password/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeSendOtp(SendChangePasswordOtpDto dto)
        {
            var message = await _service.ChangeSendOtpAsync(GetAccountId(), dto);
            if (message != null) return BadRequest(new { message = message });
            return Ok(new { message = "Mã xác nhận đã được gửi." });
        }

        [HttpPut("me/change-password/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeVerifyOtp(VerifyChangePasswordOtpDto dto)
        {
            var message = await _service.ChangeVerifyOtpAsync(GetAccountId(), dto);
            if (message != null) return BadRequest(new { message = message });
            return Ok(new { message = "Mật khẩu đã được cập nhật." });
        }

        [HttpPost("me/change-email/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeEmailSendOtp([FromBody] SendChangeEmailOtpDto dto)
        {
            var accountId = int.Parse(User.FindFirstValue("accountId")!);
            var error = await _service.ChangeEmailSendOtpAsync(accountId, dto.NewEmail);
            if (error != null) return BadRequest(new { message = error });
            return Ok(new { message = "OTP đã được gửi đến email mới." });
        }

        [HttpPut("me/change-email/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeEmailVerifyOtp([FromBody] VerifyChangeEmailOtpDto dto)
        {
            var accountId = int.Parse(User.FindFirstValue("accountId")!);
            var userId = GetUserId();
            var result = await _service.ChangeEmailVerifyOtpAsync(accountId, userId, dto.NewEmail, dto.Otp);
            if (result == null) return BadRequest(new { message = "Lỗi hệ thống." });

            dynamic res = result;
            if (res.success == false) return BadRequest(result);
            return Ok(result);
        }
    }
}