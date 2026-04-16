using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> RegisterSendOtp(SendOtpDto dto);
        Task<ApiResponse<object>> RegisterVerifyOtp(VerifyRegisterOtpDto dto);
        Task<ApiResponse<AuthResponseDto>> Login(LoginDto dto);

        Task<ApiResponse<object>> ForgotSendOtp(SendOtpDto dto);
        Task<ApiResponse<object>> ForgotVerifyOtp(VerifyForgotOtpDto dto);
        
        Task<ApiResponse<object>> GetMe(int accountId);
        Task<ApiResponse<object>> UpdateMe(int accountId, int userId, UpdateProfileDto dto);

        Task<ApiResponse<object>> ChangeSendOtp(int accountId, SendChangePasswordOtpDto dto);
        Task<ApiResponse<object>> ChangeVerifyOtp(int accountId, VerifyChangePasswordOtpDto dto);
    }
}
