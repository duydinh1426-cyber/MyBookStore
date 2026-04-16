using WebAPI.DTOs;

namespace WebAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task RegisterSendOtpAsync(SendOtpDto dto);
        Task RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);

        Task ForgotSendOtpAsync(SendOtpDto dto);
        Task ForgotVerifyOtpAsync(VerifyForgotOtpDto dto);

        Task<UserProfileDto> GetMeAsync(int accountId);
        Task<object> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto);

        Task ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto);
        Task ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto);
    }
}
