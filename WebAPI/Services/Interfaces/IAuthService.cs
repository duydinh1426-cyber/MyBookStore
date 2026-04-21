using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string?> RegisterSendOtpAsync(SendOtpDto dto);
        Task<string?> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<string?> ForgotSendOtpAsync(SendOtpDto dto);
        Task<string?> ForgotVerifyOtpAsync(VerifyForgotOtpDto dto);
        Task<UserProfileDto?> GetMeAsync(int accountId);
        Task<object?> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto);
        Task<string?> ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto);
        Task<string?> ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto);
        Task<string?> ChangeEmailSendOtpAsync(int accountId, string newEmail);
        Task<object?> ChangeEmailVerifyOtpAsync(int accountId, int userId, string newEmail, string otp);
    }
}