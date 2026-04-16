using Data.Repositories.Interfaces;
using Microsoft.IdentityModel.Tokens;
using MyBookStore.Data.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebAPI.DTOs;
using WebAPI.Enums;
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

        public async Task<ApiResponse<object>> RegisterSendOtp(SendOtpDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || !dto.Email.Contains('@'))
                return ApiResponse<object>.Fail("Email không hợp lệ.");

            if (await _repo.EmailExists(dto.Email))
                return ApiResponse<object>.Fail("Email đã tồn tại.");

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.REGISTER);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.REGISTER);

            return ApiResponse<object>.Success(null, "OTP đã được gửi đến email của bạn.");
        }

        public async Task<ApiResponse<object>> RegisterVerifyOtp(VerifyRegisterOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.REGISTER))
                return ApiResponse<object>.Fail("OTP không hợp lệ.");

            if (await _repo.UserNameExists(dto.Username))
                return ApiResponse<object>.Fail("Username đã tồn tại.");

            if (dto.Password.Length < 6)
                return ApiResponse<object>.Fail("Password phải có ít nhất 6 ký tự.");

            var account = await _repo.CreateAccount(new Account
            {
                Username = dto.Username,
                Password = HashPassword(dto.Password),
                Email = dto.Email,
                IsAdmin = false,
            });

            await _repo.CreateCustomer(new Customer
            {
                AccountId = account.AccountId,
                Name = dto.Name,
                Address = dto.Address
            });

            return ApiResponse<object>.Success(null, "Đăng ký thành công.");
        }

        public async Task<ApiResponse<AuthResponseDto>> Login(LoginDto dto)
        {
            var account = await _repo.GetByUsername(dto.Username);

            if (account == null || account.Password != HashPassword(dto.Password))
                return ApiResponse<AuthResponseDto>.Fail("Sai tài khoản hoặc mật khẩu.", 401);

            var admin = account.Admins.FirstOrDefault();
            var customer = account.Customers.FirstOrDefault();

            var userId = account.IsAdmin ? admin?.UserId : customer?.UserId;
            var name = account.IsAdmin ? admin?.Name : customer?.Name;

            if (userId == null)
                return ApiResponse<AuthResponseDto>.Fail("Tài khoản không hợp lệ.", 401);

            var token = GenerateJwt(account, userId.Value, name ?? "");

            return ApiResponse<AuthResponseDto>.Success(
                new AuthResponseDto(token, account.AccountId, userId.Value, name ?? "", account.IsAdmin)
            );
        }

        public async Task<ApiResponse<object>> ForgotSendOtp(SendOtpDto dto)
        {
            var account = await _repo.GetByEmail(dto.Email);

            if (account != null)
            {
                var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.FORGOT_PASSWORD);
                await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.FORGOT_PASSWORD);
            }

            return ApiResponse<object>.Success(null, "OTP đã được gửi đến email của bạn.");
        }

        public async Task<ApiResponse<object>> ForgotVerifyOtp(VerifyForgotOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.FORGOT_PASSWORD))
                return ApiResponse<object>.Fail("OTP không hợp lệ.");

            var account = await _repo.GetByEmail(dto.Email);
            if (account == null)
                return ApiResponse<object>.Fail("Email không tồn tại.");

            account.Password = HashPassword(dto.NewPassword);
            await _repo.UpdateAccount(account);

            return ApiResponse<object>.Success(null, "Đổi mật khẩu thành công.");
        }

        public async Task<ApiResponse<object>> GetMe(int accountId)
        {
            var account = await _repo.GetById(accountId);

            if (account == null)
                return ApiResponse<object>.Fail("Tài khoản không tồn tại.", 404);

            var customer = account.Customers.FirstOrDefault();
            var admin = account.Admins.FirstOrDefault();

            if (account.IsAdmin)
                return ApiResponse<object>.Success(new
                {
                    accountId = account.AccountId,
                    username = account.Username,
                    email = account.Email,
                    name = admin?.Name ?? "",
                    isAdmin = true,
                    createdAt = account.CreatedAt
                });

            return ApiResponse<object>.Success(new
            {
                accountId = account.AccountId,
                username = account.Username,
                email = account.Email,
                name = customer?.Name ?? "",
                address = customer?.Address ?? "",
                isAdmin = false,
                createdAt = account.CreatedAt
            });
        }

        public async Task<ApiResponse<Object>> UpdateMe(int accountId, int userId, UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponse<object>.Fail("Họ tên không được để trống.");

            var account = await _repo.GetById(accountId);
            if (account == null)
                return ApiResponse<object>.Fail("Tài khoản không tồn tại.", 404);

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != account.Email)
            {
                if (await _repo.EmailExists(dto.Email))
                    return ApiResponse<object>.Fail("Email đã được sử dụng ở tài khoản khác.");
                account.Email = dto.Email.Trim();
            }

            var customer = account.Customers.FirstOrDefault();
            if (customer != null)
            {
                customer.Name = dto.Name.Trim();
                customer.Address = dto.Address?.Trim() ?? "";
            }

            await _repo.UpdateAccount(account);

            var newToken = GenerateJwt(account, userId, customer?.Name ?? "");

            return ApiResponse<object>.Success(new
            {
                token = newToken,
                name = customer?.Name ?? "",
                email = account.Email,
                address = customer?.Address ?? ""
            }, "Cập nhật thông tin thành công.");
        }

        public async Task<ApiResponse<Object>> ChangeSendOtp(int accountId, SendChangePasswordOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                return ApiResponse<object>.Fail("Vui lòng nhập mật khẩu hiện tại.");

            var account = await _repo.GetById(accountId);
            if (account == null)
                return ApiResponse<object>.Fail("Tài khoản không tồn tại.");

            if (account.Password != HashPassword(dto.CurrentPassword))
                return ApiResponse<object>.Fail("Mật khẩu hiện tại không đúng.");

            if (string.IsNullOrEmpty(account.Email))
                return ApiResponse<object>.Fail("Tài khoản không có email để gửi OTP.");

            var otp = _otp.GenerateOtp(account.Email, OtpPurpose.CHANGE_PASSWORD);
            await _email.SendOtpAsync(account.Email, otp, OtpPurpose.CHANGE_PASSWORD);

            return ApiResponse<object>.Success(null, "Đã gửi OTP.");
        }

        public async Task<ApiResponse<Object>> ChangeVerifyOtp(int accountId, VerifyChangePasswordOtpDto dto)
        {
            var account = await _repo.GetById(accountId);
            if (account == null)
                return ApiResponse<object>.Fail("Tài khoản không tồn tại.");

            if (!_otp.VerifyOtp(account.Email ?? "", dto.Otp, OtpPurpose.CHANGE_PASSWORD))
                return ApiResponse<object>.Fail("OTP không hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return ApiResponse<object>.Fail("Mật khẩu mới phải có ít nhất 6 ký tự.");

            if (dto.NewPassword != dto.ConfirmPassword)
                return ApiResponse<object>.Fail("Xác nhận mật khẩu không khớp.");

            if (account.Password == HashPassword(dto.NewPassword))
                return ApiResponse<object>.Fail("Mật khẩu mới phải khác mật khẩu cũ.");

            account.Password = HashPassword(dto.NewPassword);
            await _repo.UpdateAccount(account);

            return ApiResponse<object>.Success(null, "Đổi mật khẩu thành công.");
        }
    }
}