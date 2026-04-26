using WebAPI.Common;
using WebAPI.DTOs;

namespace WebAPI.Services.Auth
{
    public interface IAuthService
    {
        Task<ApiResponse<object?>> RegisterSendOtpAsync(string email);
        Task<ApiResponse<object?>> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto);
        Task<ApiResponse<AuthResponseDto?>> LoginAsync(string email, string password);
        Task<ApiResponse<object?>> ForgotSendOtpAsync(string email);
        Task<ApiResponse<object?>> ForgotVerifyOtpAsync(string email, string otp, string newPass, string confirmPass);
        Task<ApiResponse<UserProfileDto?>> GetMeAsync(int accountId);
        Task<ApiResponse<UpdateProfileResponseDto?>> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto);
        Task<ApiResponse<object?>> ChangeSendOtpAsync(int accountId, string currentPass);
        Task<ApiResponse<object?>> ChangeVerifyOtpAsync(int accountId, string otp, string newPass, string confirmPass);
        Task<ApiResponse<object?>> ChangeEmailSendOtpAsync(int accountId, string newEmail);
        Task<ApiResponse<ChangeEmailResponseDto?>> ChangeEmailVerifyOtpAsync(int accountId, int userId, string newEmail, string otp);
    }
}