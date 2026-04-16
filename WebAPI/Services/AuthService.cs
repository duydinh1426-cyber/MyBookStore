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

        public async Task RegisterSendOtpAsync(SendOtpDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || !dto.Email.Contains('@'))
                throw new ArgumentException("Email không hợp lệ.");

            if (await _repo.IsEmailExistsAsync(dto.Email))
                throw new ArgumentException("Email đã tồn tại.");

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.REGISTER);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.REGISTER);
        }

        public async Task RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.REGISTER))
                throw new ArgumentException("OTP không hợp lệ.");

            if (await _repo.IsUsernameExistsAsync(dto.Username))
                throw new ArgumentException("Username đã tồn tại.");

            if (dto.Password.Length < 6)
                throw new ArgumentException("Password phải có ít nhất 6 ký tự.");

            var account = new Account
            {
                Username = dto.Username,
                Password = HashPassword(dto.Password),
                Email = dto.Email,
                IsAdmin = false,
            };

            _repo.AddAccount(account);

            var customer = new Customer
            {
                Account = account,
                Name = dto.Name,
                Address = dto.Address
            };
            _repo.AddCustomer(customer);

            if (!await _repo.SaveChangesAsync())
                throw new Exception("Lỗi hệ thống khi tạo tài khoản.");
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var account = await _repo.GetByUsernameAsync(dto.Username);

            if (account == null || account.Password != HashPassword(dto.Password))
                throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu.");

            var admin = account.Admins.FirstOrDefault();
            var customer = account.Customers.FirstOrDefault();

            var userId = account.IsAdmin ? admin?.UserId : customer?.UserId;
            var name = account.IsAdmin ? admin?.Name : customer?.Name;

            if (userId == null)
                throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");

            var token = GenerateJwt(account, userId.Value, name ?? "");

            return new AuthResponseDto(token, account.AccountId, userId.Value, name ?? "", account.IsAdmin);
        }

        public async Task ForgotSendOtpAsync(SendOtpDto dto)
        {
            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null) throw new ArgumentException("Email không tồn tại trên hệ thống.");

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.FORGOT_PASSWORD);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.FORGOT_PASSWORD);
        }

        public async Task ForgotVerifyOtpAsync(VerifyForgotOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.FORGOT_PASSWORD))
                throw new ArgumentException("OTP không hợp lệ.");

            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null) throw new ArgumentException("Tài khoản không tồn tại.");

            account.Password = HashPassword(dto.NewPassword);
            _repo.UpdateAccount(account);

            await _repo.SaveChangesAsync();
        }

        public async Task<UserProfileDto> GetMeAsync(int accountId)
        {
            var account = await _repo.GetByIdAsync(accountId);

            if (account == null)
                throw new KeyNotFoundException("Tài khoản không tồn tại.");

            var customer = account.Customers.FirstOrDefault();
            var admin = account.Admins.FirstOrDefault();

            return new UserProfileDto(
                account.AccountId,
                account.Username ?? "",
                account.Email ?? "",
                account.IsAdmin ? admin?.Name : customer?.Name,
                account.IsAdmin ? "" : customer?.Address,
                account.IsAdmin,
                account.CreatedAt
            );
        }

        public async Task<object> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Họ tên không được để trống.");

            var account = await _repo.GetByIdAsync(accountId);
            if (account == null)
                throw new KeyNotFoundException("Tài khoản không tồn tại.");

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != account.Email)
            {
                if (await _repo.IsEmailExistsAsync(dto.Email))
                    throw new ArgumentException("Email đã được sử dụng.");
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
                token = GenerateJwt(account, userId, customer?.Name ?? ""),
                name = customer?.Name,
                email = account.Email,
                address = customer?.Address
            };
        }

        public async Task ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                throw new ArgumentException("Mật khẩu hiện tại không đúng.");

            var account = await _repo.GetByIdAsync(accountId);
            if (account == null) 
                throw new KeyNotFoundException("Tài khoản không tồn tại.");

            if (account.Password != HashPassword(dto.CurrentPassword))
                throw new ArgumentException("Mật khẩu hiện tại không đúng.");

            var otp = _otp.GenerateOtp(account.Email!, OtpPurpose.CHANGE_PASSWORD);
            await _email.SendOtpAsync(account.Email!, otp, OtpPurpose.CHANGE_PASSWORD);
        }

        public async Task ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null)
                throw new KeyNotFoundException("Tài khoản không tồn tại.");

            if (!_otp.VerifyOtp(account.Email!, dto.Otp, OtpPurpose.CHANGE_PASSWORD))
                throw new ArgumentException("OTP không hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                throw new ArgumentException("Mật khẩu mới phải có ít nhất 6 ký tự.");

            if (dto.NewPassword != dto.ConfirmPassword)
                throw new ArgumentException("Xác nhận mật khẩu không khớp.");

            if (account.Password == HashPassword(dto.NewPassword))
                throw new ArgumentException("Mật khẩu mới phải khác mật khẩu cũ.");

            account.Password = HashPassword(dto.NewPassword);
            _repo.UpdateAccount(account);

            await _repo.SaveChangesAsync();
        }
    }
}