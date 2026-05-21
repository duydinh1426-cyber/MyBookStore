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
using System.Text.Json;


namespace WebAPI.Services.Auth
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

        public async Task<ServiceResult> RegisterSendOtpAsync(OtpRequest dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || !dto.Email.Contains('@'))
                return ServiceResult.Failure("Email không hợp lệ.", 400);

            if (await _repo.IsEmailExistsAsync(dto.Email))
                return ServiceResult.Failure("Email đã tồn tại trên hệ thống.", 409);

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.REGISTER);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.REGISTER);
            return ServiceResult.Success("Mã OTP đã được gửi đến email của bạn.");
        }

        public async Task<ServiceResult> RegisterVerifyOtpAsync(VerifyRegisterOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.REGISTER))
                return ServiceResult.Failure("Mã OTP không chính xác hoặc đã hết hạn.", 400);

            if (dto.Password.Length < 6)
                return ServiceResult.Failure("Mật khẩu phải có ít nhất 6 ký tự", 400);

            var account = new Account
            {
                Password = HashPassword(dto.Password),
                Email = dto.Email,
                IsAdmin = false,
            };

            _repo.AddAccount(account);
            _repo.AddCustomer(new Customer { Account = account, Name = dto.Name, Address = dto.Address });

            if (!await _repo.SaveChangesAsync())
                return ServiceResult.Failure("Lỗi hệ thống.", 500);

            return ServiceResult.Success("Đăng ký tài khoản thành công.");
        }

        public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var account = await _repo.GetByEmailAsync(dto.Email);

            if (account == null || account.Password != HashPassword(dto.Password))
                return ServiceResult<AuthResponseDto>.Failure("Tên đăng nhập hoặc mật khẩu không đúng.", 401);

            var admin = account.Admins.FirstOrDefault();
            var customer = account.Customers.FirstOrDefault();
            var userId = account.IsAdmin ? admin?.UserId : customer?.UserId;
            var name = account.IsAdmin ? admin?.Name : customer?.Name;

            if (userId == null)
                return ServiceResult<AuthResponseDto>.Failure("Tài khoản không tồn tại", 404);

            var token = GenerateJwt(account, userId.Value, name ?? "");
            var response = new AuthResponseDto(token, account.AccountId, userId.Value, name ?? "", account.IsAdmin);

            return ServiceResult<AuthResponseDto>.Success(response);
        }

        public async Task<ServiceResult> ForgotSendOtpAsync(OtpRequest dto)
        {
            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null)
                return ServiceResult.Failure("Email không tồn tại trên hệ thống", 404);

            var otp = _otp.GenerateOtp(dto.Email, OtpPurpose.FORGOT_PASSWORD);
            await _email.SendOtpAsync(dto.Email, otp, OtpPurpose.FORGOT_PASSWORD);

            return ServiceResult.Success("Mã khôi phục đã được gửi.");
        }

        public async Task<ServiceResult> ForgotVerifyOtpAsync(VerifyForgotOtpDto dto)
        {
            if (!_otp.VerifyOtp(dto.Email, dto.Otp, OtpPurpose.FORGOT_PASSWORD))
                return ServiceResult.Failure("Mã OTP không hợp lệ.", 400);

            var account = await _repo.GetByEmailAsync(dto.Email);
            if (account == null)
                return ServiceResult.Failure("Tài khoản không tồn tại.", 404);

            account.Password = HashPassword(dto.NewPassword);
            _repo.UpdateAccount(account);
            await _repo.SaveChangesAsync();
            return ServiceResult.Success("Đổi mật khẩu thành công.");
        }

        public async Task<ServiceResult<UserProfileDto>> GetMeAsync(int accountId)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null)
                return ServiceResult<UserProfileDto>.Failure("Không tìm thấy tài khoản", 404);

            var customer = account.Customers.FirstOrDefault();
            var admin = account.Admins.FirstOrDefault();

            var data = new UserProfileDto(
                account.AccountId, account.Email ?? "",
                account.IsAdmin ? admin?.Name : customer?.Name,
                account.IsAdmin ? "" : customer?.Address,
                account.IsAdmin, account.CreatedAt
            );

            return ServiceResult<UserProfileDto>.Success(data);
        }

        public async Task<ServiceResult<UpdateProfileResponseDto>> UpdateMeAsync(int accountId, int userId, UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ServiceResult<UpdateProfileResponseDto>.Failure("Tên không được để trống.", 400);

            var account = await _repo.GetByIdAsync(accountId);
            if (account == null)
                return ServiceResult<UpdateProfileResponseDto>.Failure("Tài khoản không tồn tại trong hệ thống.", 404);

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != account.Email)
            {
                if (await _repo.IsEmailExistsAsync(dto.Email))
                    return ServiceResult<UpdateProfileResponseDto>.Failure("Email không được để trống.", 400);
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

            var data = new UpdateProfileResponseDto(

                GenerateJwt(account, userId, customer?.Name ?? ""),
                customer?.Name ?? "",
                account.Email ?? "",
                customer?.Address
            );
            return ServiceResult<UpdateProfileResponseDto>.Success(data, "Cập nhật thông tin thành công.");
        }

        public async Task<ServiceResult> ChangeSendOtpAsync(int accountId, SendChangePasswordOtpDto dto)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null)
                return ServiceResult.Failure("Tài khoản không tồn tại.", 400);

            if (account.Password != HashPassword(dto.CurrentPassword))
                return ServiceResult.Failure("Mật khẩu hiện tại không đúng.", 401);

            var otp = _otp.GenerateOtp(account.Email!, OtpPurpose.CHANGE_PASSWORD);
            await _email.SendOtpAsync(account.Email!, otp, OtpPurpose.CHANGE_PASSWORD);
            return ServiceResult.Success("Mã xác nhận đã được gửi.");
        }

        public async Task<ServiceResult> ChangeVerifyOtpAsync(int accountId, VerifyChangePasswordOtpDto dto)
        {
            var account = await _repo.GetByIdAsync(accountId);
            if (account == null)
                return ServiceResult.Failure("Tài khoản không tồn tại.", 404);

            if (!_otp.VerifyOtp(account.Email!, dto.Otp, OtpPurpose.CHANGE_PASSWORD))
                return ServiceResult.Failure("Mã OTP không hợp lệ.", 400);

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return ServiceResult.Failure("Mật khẩu mới tối thiểu 6 ký tự.", 400);

            if (dto.NewPassword != dto.ConfirmPassword)
                return ServiceResult.Failure("Xác nhận mật khẩu không khớp.", 400);

            account.Password = HashPassword(dto.NewPassword);
            _repo.UpdateAccount(account);
            await _repo.SaveChangesAsync();
            return ServiceResult.Success("Mật khẩu đã được cập nhật.");
        }

        public async Task<ServiceResult> ChangeEmailSendOtpAsync(int accountId, string newEmail)
        {
            if (string.IsNullOrEmpty(newEmail) || !newEmail.Contains('@'))
                return ServiceResult.Failure("Email không hợp lệ.", 400);

            var account = await _repo.GetByIdAsync(accountId);
            if (account == null)
                return ServiceResult.Failure("Tài khoản không tồn tại.", 404);

            if (newEmail == account.Email)
                return ServiceResult.Failure("Email mới phải khác email hiện tại.", 400);

            if (await _repo.IsEmailExistsAsync(newEmail))
                return ServiceResult.Failure("Email này đã được sử dụng bởi tài khoản khác.", 409);

            var otp = _otp.GenerateOtp(newEmail, OtpPurpose.CHANGE_EMAIL);
            await _email.SendOtpAsync(newEmail, otp, OtpPurpose.CHANGE_EMAIL);
            return ServiceResult.Success("OTP đã được gửi đến email mới.");
        }

        public async Task<ServiceResult<ChangeEmailResponseDto>> ChangeEmailVerifyOtpAsync(int accountId, int userId, string newEmail, string otp)
        {
            if (!_otp.VerifyOtp(newEmail, otp, OtpPurpose.CHANGE_EMAIL))
                return ServiceResult<ChangeEmailResponseDto>.Failure("Mã OTP không hợp lệ hoặc đã hết hạn.", 400);

            var account = await _repo.GetByIdAsync(accountId);
            if (account == null)
                return ServiceResult<ChangeEmailResponseDto>.Failure("Tài khoản không tồn tại.", 404);

            if (await _repo.IsEmailExistsAsync(newEmail))
                return ServiceResult<ChangeEmailResponseDto>.Failure("Email này đã được sử dụng.", 409);

            account.Email = newEmail.Trim();
            _repo.UpdateAccount(account);
            await _repo.SaveChangesAsync();

            var customer = account.Customers.FirstOrDefault();

            var data = new ChangeEmailResponseDto(
                GenerateJwt(account, userId, customer?.Name ?? ""),
                account.Email
                );

            return ServiceResult<ChangeEmailResponseDto>.Success(data, "Cập nhật email thành công.");
        }
        public async Task<ServiceResult<AuthResponseDto>> GoogleLoginAsync(GoogleLoginDto dto)
        {

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dto.IdToken);
            var response = await httpClient.GetAsync(
                "https://www.googleapis.com/oauth2/v3/userinfo");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<AuthResponseDto>.Failure("Google token không hợp lệ.", 401);

            var json = await response.Content.ReadAsStringAsync();
            var payload = JsonDocument.Parse(json).RootElement;

            var email = payload.GetProperty("email").GetString();
            var name = payload.TryGetProperty("name", out var nameProp)
                       ? nameProp.GetString() : email;

            if (string.IsNullOrEmpty(email))
                return ServiceResult<AuthResponseDto>.Failure("Không lấy được email từ Google.", 400);


            var account = await _repo.GetByEmailAsync(email);

            if (account == null)
            {

                account = new Account
                {
                    Email = email,
                    Password = HashPassword(Guid.NewGuid().ToString()),
                    IsAdmin = false,
                };

                _repo.AddAccount(account);
                _repo.AddCustomer(new Customer { Account = account, Name = name ?? email, Address = "" });

                if (!await _repo.SaveChangesAsync())
                    return ServiceResult<AuthResponseDto>.Failure("Lỗi hệ thống.", 500);


                account = await _repo.GetByEmailAsync(email);
            }

            var customer = account!.Customers.FirstOrDefault();
            var admin = account.Admins.FirstOrDefault();
            var userId = account.IsAdmin ? admin?.UserId : customer?.UserId;
            var displayName = account.IsAdmin ? admin?.Name : customer?.Name;

            if (userId == null)
                return ServiceResult<AuthResponseDto>.Failure("Tài khoản không hợp lệ.", 404);

            var token = GenerateJwt(account, userId.Value, displayName ?? "");
            var result = new AuthResponseDto(token, account.AccountId, userId.Value, displayName ?? "", account.IsAdmin);

            return ServiceResult<AuthResponseDto>.Success(result);
        }
    }

}