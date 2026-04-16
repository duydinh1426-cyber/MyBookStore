namespace WebAPI.DTOs
{
    public record SendOtpDto(string Email); // gửi OTP đăng ký/quên mk
    public record VerifyRegisterOtpDto( // xác thực OTP đăng ký
        string Email,
        string Otp,
        string Username,
        string Password,
        string Name,
        string? Address
    ); 
    public record LoginDto( // đăng nhập
        string Username, 
        string Password
    );
    public record UpdateProfileDto( // cập nhật thông tin cá nhân
        string Name,
        string? Email,
        string? Address
    ); 
    public record VerifyForgotOtpDto( // xác thực OTP quên mk
        string Email,
        string Otp,
        string NewPassword,
        string ConfirmPassword
    );
    public record SendChangePasswordOtpDto(string CurrentPassword); // bước 1 đổi mật khẩu
    public record VerifyChangePasswordOtpDto( // bước 2 đổi mật khẩu
        string Otp,
        string NewPassword,
        string ConfirmPassword
    );
    public record AuthResponseDto(
        string Token,
        int AccountId,
        int UserId,
        string Name,
        bool IsAdmin
    );
}
