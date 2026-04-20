using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebAPI.DTOs;
using WebAPI.Enums;
using WebAPI.Services.Helper;
using WebAPI.Services.Interfaces;
using Data;

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

        public async Task<string?> RegisterSendOtpAsync(SendOtpDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || !dto.Email.Contains('@'))
                return "Email không hợp lệ.";

            if (await _repo.IsEmailExistsAsync(dto.Email))
                return "Email đã tồn tại trên hệ thống.";

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.REGISTER);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.REGISTER);
            return null;
        }

        public async Task<string?> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.REGISTER))
                return "Mã OTP không chính xác hoặc đã hết hạn.";

            if (dto.Password.Length < 6)
                return "Mật khẩu phải có ít nhất 6 ký tự.";

            var account = new Account
            {
                Password = HashPassword(dto.Password),
                Email = dto.Email,
                IsAdmin = false,
            };

            _repo.AddAccount(account);
            _repo.AddCustomer(new Customer { Account = account, Name = dto.Name, Address = dto.Address });

            if (!await _repo.SaveChangesAsync())
                return "Lỗi hệ thống khi tạo tài khoản.";

            return null;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null || account.Password != HashPassword(dto.Password))
                return null;

            var admin = account.Admins.FirstOrDefault();
            var customer = account.Customers.FirstOrDefault();
            var userId = account.IsAdmin ? admin?.UserId : customer?.UserId;
            var name = account.IsAdmin ? admin?.Name : customer?.Name;

            if (userId == null) return null;

            var token = GenerateJwt(account, userId.Value, name ?? "");
            return new AuthResponseDto(token, account.AccountId, userId.Value, name ?? "", account.IsAdmin);
        }

        public async Task<string?> ForgotSendOtpAsync(SendOtpDto dto)
        {
            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null) return "Email không tồn tại trên hệ thống.";

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.FORGOT_PASSWORD);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.FORGOT_PASSWORD);
            return null;
        }

        public async Task<string?> ForgotVerifyOtpAsync(VerifyForgotOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.FORGOT_PASSWORD))
                return "Mã OTP không hợp lệ.";

            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null) return "Tài khoản không tồn tại.";

            account.Password = HashPassword(dto.NewPassword);
            _repo.UpdateAccount(account);
            await _repo.SaveChangesAsync();
            return null;
        }

        public async Task<UserProfileDto?> GetMeAsync(int accountId)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) return null;

            var customer = account.Customers.FirstOrDefault();
            var admin = account.Admins.FirstOrDefault();

            return new UserProfileDto(
                account.AccountId, account.Email ?? "",
                account.IsAdmin ? admin?.Name : customer?.Name,
                account.IsAdmin ? "" : customer?.Address,
                account.IsAdmin, account.CreatedAt
            );
        }

        public async Task<object?> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return null;

            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != account.Email)
            {
                if (await _repo.IsEmailExistsAsync(dto.Email)) return null;
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

            return new
            {
                message = "Cập nhật thông tin thành công.",
                token = GenerateJwt(account, userId, customer?.Name ?? ""),
                name = customer?.Name,
                email = account.Email,
                address = customer?.Address
            };
        }

        public async Task<string?> ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) return "Tài khoản không tồn tại.";

            if (account.Password != HashPassword(dto.CurrentPassword))
                return "Mật khẩu hiện tại không đúng.";

            var otp = _otp.GenerateOtp(account.Email!, OtpPurpose.CHANGE_PASSWORD);
            await _email.SendOtpAsync(account.Email!, otp, OtpPurpose.CHANGE_PASSWORD);
            return null;
        }

        public async Task<string?> ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) return "Tài khoản không tồn tại.";

            if (!_otp.VerifyOtp(account.Email!, dto.Otp, OtpPurpose.CHANGE_PASSWORD))
                return "Mã OTP không hợp lệ.";

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return "Mật khẩu mới tối thiểu 6 ký tự.";

            if (dto.NewPassword != dto.ConfirmPassword)
                return "Xác nhận mật khẩu không khớp.";

            account.Password = HashPassword(dto.NewPassword);
            _repo.UpdateAccount(account);
            await _repo.SaveChangesAsync();
            return null;
        }
    }
}