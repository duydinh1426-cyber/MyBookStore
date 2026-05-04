using Microsoft.Extensions.Caching.Memory;
using WebAPI.Enums;

namespace WebAPI.Services.Helper
{
    public interface IOtpService
    {
        string GenerateOtp(string email, OtpPurpose purpose);  // tạo mã OTP ngẫu nhiên
        bool VerifyOtp(string email, string otp, OtpPurpose purpose); // xác thực mã OTP
    }

    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private static readonly Random _rng = Random.Shared;

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateOtp(string email, OtpPurpose purpose)
        {
            var otp = _rng.Next(100000, 999999).ToString(); // tạo mã OTP 6 chữ số
            _cache.Set(GetCacheKey(email, purpose), otp, TimeSpan.FromMinutes(5)); // lưu mã OTP vào cache với thời gian hết hạn 5 phút
            return otp;
        }

        public bool VerifyOtp(string email, string otp, OtpPurpose purpose)
        {
            string key = GetCacheKey(email, purpose);
            if (_cache.TryGetValue(key, out string? storeOtp) && storeOtp == otp)
            {
                _cache.Remove(key); // xóa mã OTP sau khi xác thực thành công
                return true;
            }
            return false;
        }

        private string  GetCacheKey(string email, OtpPurpose purpose)
        {
            return $"otp:{purpose}:{email}"; // tạo khóa cache dựa trên mục đích và email
        }
    }
}
