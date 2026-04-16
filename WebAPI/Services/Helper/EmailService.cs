using MimeKit;
using MailKit.Net.Smtp;
using WebAPI.Enums;

namespace WebAPI.Services.Helper
{
    public interface IEmailService
    {
        Task SendOtpAsync(string toEmail, string otp, OtpPurpose purpose);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _cfg;
        public EmailService(IConfiguration cfg)
        {
            _cfg = cfg;
        }

        public async Task SendOtpAsync(string toEmail, string otp, OtpPurpose purpose)
        {
            var (subject, title) = purpose switch
            {
                OtpPurpose.REGISTER => ("Xác nhận đăng ký", "Đăng ký tài khoản"),
                OtpPurpose.FORGOT_PASSWORD => ("Khôi phục mật khẩu", "Đặt lại mật khẩu"),
                OtpPurpose.CHANGE_PASSWORD => ("Thay đổi mật khẩu", "Xác nhận đổi mật khẩu"),
                _ => ("Mã xác thực", "Xác thưc hệ thống")
            };

            var body = $"""
                <div style="font-family: Arial, sans-serif; border: 1px solid #ddd; padding: 20px;">
                    <h2 style="color: #007bff;">Mã xác thực từ BookStore</h2>
                    <p>Bạn đã yêu cầu mã OTP để <strong>{title}</strong>.</p>
                    <div style="background: #f4f4f4; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px;">
                        {otp}
                    </div>
                    <p>Mã này có hiệu lực trong 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                </div>
                """;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("BookStore System", _cfg["Email:From"] ?? ""));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_cfg["Email:Host"], int.Parse(_cfg["Email:Port"]!), MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_cfg["Email:Username"], _cfg["Email:Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
