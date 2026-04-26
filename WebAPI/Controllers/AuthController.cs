using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Services.Auth;

namespace WebAPI.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service) => _service = service;

        [HttpPost("register/send-otp")]
        public async Task<IActionResult> RegisterSendOtp(string email)
            => FromResponse(await _service.RegisterSendOtpAsync(email));

        [HttpPost("register/verify-otp")]
        public async Task<IActionResult> RegisterVerifyOtp(VerifyRegisterOtpDto dto)
            => FromResponse(await _service.RegisterVerifyOtpAsync(dto)); 

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
            => FromResponse(await _service.LoginAsync(email, password));

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> ForgotSendOtp(string email)
            => FromResponse(await _service.ForgotSendOtpAsync(email));

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> ForgotVerifyOtp(string email, string otp, string newPass, string confirmPass)
            => FromResponse(await _service.ForgotVerifyOtpAsync(email, otp, newPass, confirmPass));

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
            => FromResponse(await _service.GetMeAsync(AccountId));

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
            => FromResponse(await _service.UpdateMeAsync(AccountId, UserId, dto));

        [HttpPost("me/change-password/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeSendOtp(string currentPass)
            => FromResponse(await _service.ChangeSendOtpAsync(AccountId, currentPass));

        [HttpPut("me/change-password/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeVerifyOtp(string otp, string newPass, string confirmPass)
            => FromResponse(await _service.ChangeVerifyOtpAsync(AccountId, otp, newPass, confirmPass));

        [HttpPost("me/change-email/send-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeEmailSendOtp([FromBody] string newEmail)
            => FromResponse(await _service.ChangeEmailSendOtpAsync(AccountId, newEmail));

        [HttpPut("me/change-email/verify-otp")]
        [Authorize]
        public async Task<IActionResult> ChangeEmailVerifyOtp([FromBody] string newEmail, string otp)
            => FromResponse(await _service.ChangeEmailVerifyOtpAsync(AccountId, UserId, newEmail, otp));
    }
}