using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> RegisterSendOtpAsync(SendOtpDto dto);
        Task<ApiResponse<object>> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<ApiResponse<object>> ForgotSendOtpAsync(SendOtpDto dto);
        Task<ApiResponse<object>> ForgotVerifyOtpAsync(VerifyForgotOtpDto dto);
        Task<ApiResponse<UserProfileDto>> GetMeAsync(int accountId);
        Task<ApiResponse<object>> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto);
        Task<ApiResponse<object>> ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto);
        Task<ApiResponse<object>> ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto);
    }
}
