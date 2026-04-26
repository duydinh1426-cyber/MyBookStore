namespace WebAPI.DTOs
{
    public record UserProfileDto(
    int AccountId,
    string Email,
    string? Name,
    string? Address,
    bool IsAdmin,
    DateTime CreatedAt
    );
    public record VerifyRegisterOtpDto( // xác thực OTP đăng ký
        string Email,
        string Otp,
        string Password,
        string Name,
        string? Address
    ); 
    public record UpdateProfileDto( // cập nhật thông tin cá nhân
        string Name,
        string? Email,
        string? Address
    );
    public record ChangeEmailResponseDto( // bước 2 đổi mật khẩu
        string token,
        string email
    );
    public record AuthResponseDto(
        string Token,
        int AccountId,
        int UserId,
        string Name,
        bool IsAdmin
    );
    public record UpdateProfileResponseDto(
        string Token,
        string Name,
        string Email,
        string? Address
    );
}
