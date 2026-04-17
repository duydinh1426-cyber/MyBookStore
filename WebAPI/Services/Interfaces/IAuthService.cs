using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<string>> RegisterSendOtpAsync(SendOtpDto dto);
        Task<ApiResponse<string>> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<ApiResponse<string>> ForgotSendOtpAsync(SendOtpDto dto);
        Task<ApiResponse<string>> ForgotVerifyOtpAsync(VerifyForgotOtpDto dto);
        Task<ApiResponse<UserProfileDto>> GetMeAsync(int accountId);
        Task<ApiResponse<object>> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto);
        Task<ApiResponse<string>> ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto);
        Task<ApiResponse<string>> ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto);
    }
}
