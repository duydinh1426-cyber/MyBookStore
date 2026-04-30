using WebAPI.DTOs;

namespace WebAPI.Services.Auth
{
    public interface IAuthService
    {
        Task<ServiceResult> RegisterSendOtpAsync(SendOtpDto dto);
        Task<ServiceResult> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto);
        Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<ServiceResult> ForgotSendOtpAsync(SendOtpDto dto);
        Task<ServiceResult> ForgotVerifyOtpAsync(VerifyForgotOtpDto dto);
        Task<ServiceResult<UserProfileDto>> GetMeAsync(int accountId);
        Task<ServiceResult<UpdateProfileResponseDto>> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto);
        Task<ServiceResult> ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto);
        Task<ServiceResult> ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto);
        Task<ServiceResult> ChangeEmailSendOtpAsync(int accountId, string newEmail);
        Task<ServiceResult<ChangeEmailResponseDto>> ChangeEmailVerifyOtpAsync(int accountId, int userId, string newEmail, string otp);
    }
}