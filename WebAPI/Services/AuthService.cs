using Data.Repositories.Interfaces;
using Microsoft.IdentityModel.Tokens;
using MyBookStore.Data.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebAPI.DTOs;
using WebAPI.Enums;
using WebAPI.Services.Helper;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _cfg;
        private readonly IEmailService _email;
        private readonly IOtpService _otp;

        public AuthService(IAuthRepository repo, IConfiguration cfg, IEmailService email, IOtpService otp)
        {
            _repo = repo;
            _cfg = cfg;
            _email = email;
            _otp = otp;

        }

        #region Helpers
        private string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

        private string GenerateJwt(Account account, int userId, string name)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:SecretKey"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("accountId", account.AccountId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, name ?? ""),
                new Claim(ClaimTypes.Email, account.Email ?? ""),
                new Claim(ClaimTypes.Role, account.IsAdmin ? "Admin" : "Customer")
            };

            var token = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"],
                audience: _cfg["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion

        public async Task<ApiResponse<string>> RegisterSendOtpAsync(SendOtpDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || !dto.Email.Contains('@'))
                return ApiResponse<string>.Fail("Email không hợp lệ.");

            if (await _repo.IsEmailExistsAsync(dto.Email))
                return ApiResponse<string>.Fail("Email đã tồn tại trên hệ thống.");

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.REGISTER);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.REGISTER);

            return ApiResponse<string>.Success(null, "Mã OTP đã được gửi đến email.");
        }

        public async Task<ApiResponse<string>> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.REGISTER))
                return ApiResponse<string>.Fail("Mã OTP không chính xác hoặc đã hết hạn.");

            if (await _repo.IsUsernameExistsAsync(dto.Username))
                return ApiResponse<string>.Fail("Tên đăng nhập đã tồn tại.");

            if (dto.Password.Length < 6)
                return ApiResponse<string>.Fail("Mật khẩu phải có ít nhất 6 ký tự.");

            var account = new Account
            {
                Username = dto.Username,
                Password = HashPassword(dto.Password),
                Email = dto.Email,
                IsAdmin = false,
            };

            _repo.AddAccount(account);
            _repo.AddCustomer(new Customer { Account = account, Name = dto.Name, Address = dto.Address });

            if (!await _repo.SaveChangesAsync())
                return ApiResponse<string>.Fail("Lỗi hệ thống khi tạo tài khoản.", 500);

            return ApiResponse<string>.Success(null, "Đăng ký tài khoản thành công.");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var account = await _repo.GetByUsernameAsync(dto.Username);
            if (account == null || account.Password != HashPassword(dto.Password))
                return ApiResponse<AuthResponseDto>.Fail("Tên đăng nhập hoặc mật khẩu không đúng.", 401);

            var admin = account.Admins.FirstOrDefault();
            var customer = account.Customers.FirstOrDefault();

            var userId = account.IsAdmin ? admin?.UserId : customer?.UserId;
            var name = account.IsAdmin ? admin?.Name : customer?.Name;

            if (userId == null)
                return ApiResponse<AuthResponseDto>.Fail("Tài khoản không có thông tin định danh.", 403);

            var token = GenerateJwt(account, userId.Value, name ?? "");
            var data = new AuthResponseDto(token, account.AccountId, userId.Value, name ?? "", account.IsAdmin);

            return ApiResponse<AuthResponseDto>.Success(data, "Đăng nhập thành công.");
        }

        public async Task<ApiResponse<string>> ForgotSendOtpAsync(SendOtpDto dto)
        {
            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null) return ApiResponse<string>.Fail("Email không tồn tại trên hệ thống.");

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.FORGOT_PASSWORD);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.FORGOT_PASSWORD);

            return ApiResponse<string>.Success(null, "Mã khôi phục đã được gửi.");
        }

        public async Task<ApiResponse<string>> ForgotVerifyOtpAsync(VerifyForgotOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.FORGOT_PASSWORD))
                return ApiResponse<string>.Fail("Mã OTP không hợp lệ.");

            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null) return ApiResponse<string>.Fail("Tài khoản không tồn tại.");

            account.Password = HashPassword(dto.NewPassword);
            _repo.UpdateAccount(account);
            await _repo.SaveChangesAsync();

            return ApiResponse<string>.Success(null, "Đổi mật khẩu thành công.");
        }

        public async Task<ApiResponse<UserProfileDto>> GetMeAsync(int accountId)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) return ApiResponse<UserProfileDto>.Fail("Không tìm thấy tài khoản.", 404);

            var customer = account.Customers.FirstOrDefault();
            var admin = account.Admins.FirstOrDefault();

            var profile = new UserProfileDto(
                account.AccountId, account.Username ?? "", account.Email ?? "",
                account.IsAdmin ? admin?.Name : customer?.Name,
                account.IsAdmin ? "" : customer?.Address,
                account.IsAdmin, account.CreatedAt
            );

            return ApiResponse<UserProfileDto>.Success(profile);
        }

        public async Task<ApiResponse<object>> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponse<object>.Fail("Họ tên không được để trống.");

            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) return ApiResponse<object>.Fail("Tài khoản không tồn tại.", 404);

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != account.Email)
            {
                if (await _repo.IsEmailExistsAsync(dto.Email))
                    return ApiResponse<object>.Fail("Email này đã được sử dụng bởi người khác.");
                account.Email = dto.Email.Trim();
            }

            var customer = account.Customers.FirstOrDefault();
            if (customer != null)
            {
                customer.Name = dto.Name.Trim();
                customer.Address = dto.Address?.Trim() ?? "";
            }

            _repo.UpdateAccount(account);
            await _repo.SaveChangesAsync();

            var result = new
            {
                token = GenerateJwt(account, userId, customer?.Name ?? ""),
                name = customer?.Name,
                email = account.Email,
                address = customer?.Address
            };

            return ApiResponse<object>.Success(result, "Cập nhật thông tin thành công.");
        }

        public async Task<ApiResponse<string>> ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) return ApiResponse<string>.Fail("Tài khoản không tồn tại.", 404);

            if (account.Password != HashPassword(dto.CurrentPassword))
                return ApiResponse<string>.Fail("Mật khẩu hiện tại không đúng.");

            var otp = _otp.GenerateOtp(account.Email!, OtpPurpose.CHANGE_PASSWORD);
            await _email.SendOtpAsync(account.Email!, otp, OtpPurpose.CHANGE_PASSWORD);

            return ApiResponse<string>.Success(null, "Mã xác nhận đã được gửi.");
        }

        public async Task<ApiResponse<string>> ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) return ApiResponse<string>.Fail("Tài khoản không tồn tại.", 404);

            if (!_otp.VerifyOtp(account.Email!, dto.Otp, OtpPurpose.CHANGE_PASSWORD))
                return ApiResponse<string>.Fail("Mã OTP không hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return ApiResponse<string>.Fail("Mật khẩu mới phải có ít nhất 6 ký tự.");

            if (dto.NewPassword != dto.ConfirmPassword)
                return ApiResponse<string>.Fail("Xác nhận mật khẩu không khớp.");

            if (account.Password == HashPassword(dto.NewPassword))
                return ApiResponse<string>.Fail("Mật khẩu mới phải khác mật khẩu hiện tại.");

            account.Password = HashPassword(dto.NewPassword);
            _repo.UpdateAccount(account);
            await _repo.SaveChangesAsync();

            return ApiResponse<string>.Success(null, "Mật khẩu đã được cập nhật.");
        }
    }
}