using Data.Repositories.Interfaces;
using Microsoft.IdentityModel.Tokens;
using MyBookStore.Data.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebAPI.Common;
using WebAPI.DTOs;
using WebAPI.Enums;
using WebAPI.Services.Helper;

namespace WebAPI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly IConfiguration _cfg;
        private readonly IEmailService _email;
        private readonly IOtpService _otp;

        #region Helper
        public AuthService(IAuthRepository authRepo, IConfiguration cfg, IEmailService email, IOtpService otp)
        {
            _authRepo = authRepo;
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
        #endregion

        #region Register
        public async Task<ApiResponse<object?>> RegisterSendOtpAsync(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return ApiResponse<object?>.Fail("Email không hợp lệ.", 400);

            if (await _authRepo.IsEmailExistsAsync(email))
                return ApiResponse<object?>.Fail("Email đã tồn tại trên hệ thống.", 409);

            var otp = _otp.GenerateOtp(email, OtpPurpose.REGISTER);
            await _email.SendOtpAsync(email, otp, OtpPurpose.REGISTER);
            return ApiResponse<object?>.Ok(null, "Mã OTP đã được gửi đến email.");
        }

        public async Task<ApiResponse<object?>> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.REGISTER))
                return ApiResponse<object?>.Fail("Mã OTP không chính xác hoặc đã hết hạn.", 400);

            if (dto.Password.Length < 6)
                return ApiResponse<object?>.Fail("Mật khẩu phải có ít nhất 6 ký tự.", 400);

            var account = new Account
            {
                Password = HashPassword(dto.Password),
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                IsAdmin = false,
            };

            _authRepo.AddAccount(account);
            _authRepo.AddCustomer(new Customer 
                { 
                    Account = account, 
                    Name = dto.Name, 
                    Address = dto.Address 
                });

            if (!await _authRepo.SaveChangesAsync())
                return ApiResponse<object?>.Fail("Lỗi hệ thống khi tạo tài khoản.", 500);

            return ApiResponse<object?>.Ok("Đăng ký tài khoàn thành công");
        }
        #endregion

        #region Login
        public async Task<ApiResponse<AuthResponseDto?>> LoginAsync(string email, string password)
        {
            var account = await _authRepo.GetByEmailAsync(email);

            Console.WriteLine("abc");

            if (account == null || account.Password != HashPassword(password))
                return ApiResponse<AuthResponseDto?>.Fail("Tên đăng nhập hoặc mật khẩu không đúng ABCCCCC.", 401);

            var admin = account.Admins.FirstOrDefault();
            var customer = account.Customers.FirstOrDefault();
            var userId = account.IsAdmin ? admin?.UserId : customer?.UserId;
            var name = account.IsAdmin ? admin?.Name : customer?.Name;

            if (userId == null)
                return ApiResponse<AuthResponseDto?>.Fail("Tài khoản không hợp lệ.", 500);

            var token = GenerateJwt(account, userId.Value, name ?? "");
            return ApiResponse<AuthResponseDto?>.Ok
                (new AuthResponseDto(token, account.AccountId, userId.Value, name ?? "", account.IsAdmin));
        }
        #endregion

        #region Forgot password
        public async Task<ApiResponse<object?>> ForgotSendOtpAsync(string email)
        {
            var account = await _authRepo.GetByEmailAsync(email);
            if (account == null)
                return ApiResponse<object?>.Fail("Email không tồn tại trên hệ thống.", 404);

            var otp = _otp.GenerateOtp(email, OtpPurpose.FORGOT_PASSWORD);
            await _email.SendOtpAsync(email, otp, OtpPurpose.FORGOT_PASSWORD);
            return ApiResponse<object?>.Ok(null, "Mã khôi phục đã được gửi.");
        }

        public async Task<ApiResponse<object?>> ForgotVerifyOtpAsync(string email, string otp, string newPass, string confirmPass)
        {
            if (!_otp.VerifyOtp(email, otp, OtpPurpose.FORGOT_PASSWORD))
                return ApiResponse<object?>.Fail("Mã OTP không hợp lệ.", 400);

            var account = await _authRepo.GetByEmailAsync(email);
            if (account == null)
                return ApiResponse<object?>.Fail("Tài khoản không tồn tại.", 404);

            account.Password = HashPassword(newPass);
            _authRepo.UpdateAccount(account);
            await _authRepo.SaveChangesAsync();
            return ApiResponse<object?>.Ok(null, "Đổi mật khẩu thành công.");
        }
        #endregion

        #region Profile
        public async Task<ApiResponse<UserProfileDto?>> GetMeAsync(int accountId)
        {
            var account = await _authRepo.GetByIdAsync(accountId);
            if (account == null)
                return ApiResponse<UserProfileDto?>.Fail("Tài khoản không tồn tại.", 404);

            var customer = account.Customers.FirstOrDefault();
            var admin = account.Admins.FirstOrDefault();

            return ApiResponse<UserProfileDto?>.Ok(new UserProfileDto(
                account.AccountId,
                account.Email ?? "",
                account.IsAdmin ? admin?.Name : customer?.Name,
                account.IsAdmin ? null : customer?.Address,
                account.IsAdmin,
                account.CreatedAt));
        }

        public async Task<ApiResponse<UpdateProfileResponseDto?>> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponse<UpdateProfileResponseDto?>.Fail("Tên không được để trống.", 400);

            var account = await _authRepo.GetByIdAsync(accountId);
            if (account == null)
                return ApiResponse<UpdateProfileResponseDto?>.Fail("Tài khoản không tồn tại.", 404);

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != account.Email)
            {
                if (await _authRepo.IsEmailExistsAsync(dto.Email))
                    return ApiResponse<UpdateProfileResponseDto?>.Fail("Email đã được sử dụng bởi tài khoản khác.", 409);
                account.Email = dto.Email.Trim();
            }

            var customer = account.Customers.FirstOrDefault();
            if (customer != null)
            {
                customer.Name = dto.Name.Trim();
                customer.Address = dto.Address?.Trim() ?? "";
            }

            _authRepo.UpdateAccount(account);
            await _authRepo.SaveChangesAsync();

            return ApiResponse<UpdateProfileResponseDto?>.Ok(new UpdateProfileResponseDto(
            Token: GenerateJwt(account, userId, customer?.Name ?? ""),
            Name: customer?.Name ?? "",
            Email: account.Email ?? "",
            Address: customer?.Address),
            message: "Cập nhật thông tin thành công");
        }
        #endregion

        #region Change password
        public async Task<ApiResponse<object?>> ChangeSendOtpAsync(int accountId, string currentPass)
        {
            var account = await _authRepo.GetByIdAsync(accountId);
            if (account == null)
                return ApiResponse<object?>.Fail("Tài khoản không tồn tại.", 404);

            if (account.Password != HashPassword(currentPass))
                return ApiResponse<object?>.Fail("Mật khẩu hiện tại không đúng.", 401);

            var otp = _otp.GenerateOtp(account.Email!, OtpPurpose.CHANGE_PASSWORD);
            await _email.SendOtpAsync(account.Email!, otp, OtpPurpose.CHANGE_PASSWORD);

            return ApiResponse<object?>.Ok(null, "Mã xác nhận đã được gửi.");
        }

        public async Task<ApiResponse<object?>> ChangeVerifyOtpAsync(int accountId, string otp, string newPass, string confirmPass)
        {
            var account = await _authRepo.GetByIdAsync(accountId);
            if (account == null)
                return ApiResponse<object?>.Fail("Tài khoản không tồn tại.", 404);

            if (!_otp.VerifyOtp(account.Email!, otp, OtpPurpose.CHANGE_PASSWORD))
                return ApiResponse<object?>.Fail("Mã OTP không hợp lệ.", 400);

            if (string.IsNullOrWhiteSpace(newPass) || newPass.Length < 6)
                return ApiResponse<object?>.Fail("Mật khẩu mới tối thiểu 6 ký tự.", 400);

            if (newPass != confirmPass)
                return ApiResponse<object?>.Fail("Xác nhận mật khẩu không khớp.", 400);

            account.Password = HashPassword(newPass);
            _authRepo.UpdateAccount(account);
            await _authRepo.SaveChangesAsync();
            return ApiResponse<object?>.Ok(null, "Mật khẩu đã được cập nhật.");
        }
        #endregion

        #region Change email
        public async Task<ApiResponse<object?>> ChangeEmailSendOtpAsync(int accountId, string newEmail)
        {
            if (string.IsNullOrEmpty(newEmail) || !newEmail.Contains('@'))
                return ApiResponse<object?>.Fail("Email không hợp lệ.", 400);

            var account = await _authRepo.GetByIdAsync(accountId);
            if (account == null)
                return ApiResponse<object?>.Fail("Tài khoản không tồn tại.", 404);

            if (newEmail == account.Email)
                return ApiResponse<object?>.Fail("Email mới phải khác email hiện tại.", 400);

            if (await _authRepo.IsEmailExistsAsync(newEmail))
                return ApiResponse<object?>.Fail("Email này đã được sử dụng bởi tài khoản khác.", 409);

            var otp = _otp.GenerateOtp(newEmail, OtpPurpose.CHANGE_EMAIL);
            await _email.SendOtpAsync(newEmail, otp, OtpPurpose.CHANGE_EMAIL);

            return ApiResponse<object?>.Ok(null, "OTP đã được gửi đến email mới.");
        }

        public async Task<ApiResponse<ChangeEmailResponseDto?>> ChangeEmailVerifyOtpAsync(int accountId, int userId, string newEmail, string otp)
        {
            if (!_otp.VerifyOtp(newEmail, otp, OtpPurpose.CHANGE_EMAIL))
                return ApiResponse<ChangeEmailResponseDto?>.Fail("Mã OTP không hợp lệ hoặc đã hết hạn.", 400);

            var account = await _authRepo.GetByIdAsync(accountId);
            if (account == null)
                return ApiResponse<ChangeEmailResponseDto?>.Fail("Tài khoản không tồn tại.", 404);

            if (await _authRepo.IsEmailExistsAsync(newEmail))
                return ApiResponse<ChangeEmailResponseDto?>.Fail("Email này đã được sử dụng.", 409);

            account.Email = newEmail.Trim();
            _authRepo.UpdateAccount(account);
            await _authRepo.SaveChangesAsync();

            var customer = account.Customers.FirstOrDefault();
            return ApiResponse<ChangeEmailResponseDto?>.Ok(new ChangeEmailResponseDto(
            token: GenerateJwt(account, userId, customer?.Name ?? ""),
            email: account.Email),
            message: "Cập nhật email thành công");
        }
        #endregion
    }
}