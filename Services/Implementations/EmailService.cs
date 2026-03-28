using Darb.Api.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace Darb.Api.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string?> SendOtpEmailAsync(string to)
        {
            var otp = GenerateOtp();
            var body = GenerateEmailBody(otp);
            var subject = "رمز التحقق - درب";

            var sent = await SendEmailAsync(to, subject, body);
            return sent ? otp : null;
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string GenerateEmailBody(string otp)
        {
            return $@"
                <div dir='rtl' style='font-family: Arial, sans-serif; text-align: center; color: #333;'>
                    <h2 style='color: #003366;'>مرحباً بك في درب</h2>
                    <p>رمز التحقق الخاص بك هو:</p>
                    <div style='font-size: 24px; font-weight: bold; color: #d9534f; border: 1px solid #ddd; padding: 10px; display: inline-block; margin-top: 10px;'>
                        {otp}
                    </div>
                    <p style='margin-top: 20px;'>هذا الرمز صالح لمدة 5 دقائق فقط.</p>
                </div>";
        }

        private async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:SenderEmail"]));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart(TextFormat.Html) { Text = body };

                using var smtp = new SmtpClient();
                var host = _configuration["EmailSettings:SmtpServer"];
                var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");

                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_configuration["EmailSettings:SenderEmail"], _configuration["EmailSettings:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }
    }
}
