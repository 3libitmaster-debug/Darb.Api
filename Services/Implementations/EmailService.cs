using Darb.Api.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading.Tasks;

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
                email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:SenderEmail"] ?? ""));
                email.To.Add(MailboxAddress.Parse(to ?? ""));
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

        /// <summary>
        /// Sends a styled HTML approval email to the company notifying them about
        /// their subscription being accepted. Handles both new registration and renewal cases.
        /// </summary>
        public async Task<bool> SendSubscriptionApprovalEmailAsync(string to, string companyName, bool isRenewal, DateTime expiryDate)
        {
            var subject = isRenewal
                ? "✅ تم تجديد اشتراككم بنجاح - منصة درب"
                : "🎉 مرحباً بكم! تم قبول طلب تسجيلكم - منصة درب";

            var body = GenerateApprovalEmailBody(companyName, isRenewal, expiryDate);

            return await SendEmailAsync(to, subject, body);
        }

        /// <summary>
        /// Generates a professional, styled HTML email body for the subscription approval notification.
        /// </summary>
        private string GenerateApprovalEmailBody(string companyName, bool isRenewal, DateTime expiryDate)
        {
            var headline = isRenewal
                ? "تم تجديد اشتراككم بنجاح"
                : "تهانينا! تم قبول طلب تسجيل شركتكم";

            var introText = isRenewal
                ? $"يسعدنا إعلامكم بأنه تم تجديد اشتراك شركة <strong>{companyName}</strong> في منصة درب بنجاح."
                : $"يسعدنا الترحيب بشركة <strong>{companyName}</strong> في منصة درب! لقد تمت مراجعة طلب تسجيلكم والموافقة عليه.";

            var expiryFormatted = expiryDate.ToString("dd/MM/yyyy");

            return $@"
                <div dir='rtl' style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 10px; overflow: hidden;'>
                    <div style='background-color: #003366; padding: 20px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0; font-size: 22px;'>منصة درب للنقل</h1>
                    </div>
                    <div style='padding: 30px; color: #333333;'>
                        <h2 style='color: #003366;'>{headline}</h2>
                        <p>{introText}</p>
                        <div style='background-color: #f4f8ff; border-right: 4px solid #003366; padding: 15px; margin: 20px 0; border-radius: 5px;'>
                            <p style='margin: 5px 0;'>📅 <strong>تاريخ انتهاء الاشتراك:</strong> {expiryFormatted}</p>
                        </div>
                        <p>يمكنكم الآن تسجيل الدخول إلى حسابكم والبدء في استخدام جميع خدمات المنصة.</p>
                        <p style='margin-top: 30px; color: #666; font-size: 13px;'>شكراً لثقتكم بمنصة درب.</p>
                    </div>
                    <div style='background-color: #f0f0f0; padding: 15px; text-align: center;'>
                        <p style='margin: 0; font-size: 12px; color: #999;'>هذا البريد تم إرساله تلقائياً، يرجى عدم الرد عليه.</p>
                    </div>
                </div>";
        }

        /// <summary>
        /// Sends a styled HTML rejection email to the company notifying them about
        /// their subscription being rejected. Handles both new registration and renewal cases.
        /// </summary>
        public async Task<bool> SendSubscriptionRejectionEmailAsync(string to, string companyName, bool isRenewal)
        {
            var subject = isRenewal
                ? "❌ تحديث بشأن طلب تجديد الاشتراك - منصة درب"
                : "❌ تحديث بشأن طلب تسجيل شركتكم - منصة درب";

            var body = GenerateRejectionEmailBody(companyName, isRenewal);

            return await SendEmailAsync(to, subject, body);
        }

        /// <summary>
        /// Generates a professional, styled HTML email body for the subscription rejection notification.
        /// </summary>
        private string GenerateRejectionEmailBody(string companyName, bool isRenewal)
        {
            var headline = isRenewal
                ? "تحديث بشأن طلب تجديد الاشتراك"
                : "تحديث بشأن طلب تسجيل شركتكم";

            var introText = isRenewal
                ? $"نود إحاطتكم علماً بأن إدارة منصة درب قد راجعت طلب تجديد الاشتراك الخاص بشركة <strong>{companyName}</strong>، وللأسف تم رفض الطلب الحاصل."
                : $"نود إحاطتكم علماً بأن إدارة منصة درب قد راجعت طلب التسجيل المقدم لشركة <strong>{companyName}</strong>، وللأسف تعذر قبول الطلب الحالي بعد المراجعة.";

            return $@"
                <div dir='rtl' style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 10px; overflow: hidden;'>
                    <div style='background-color: #003366; padding: 20px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0; font-size: 22px;'>منصة درب للنقل</h1>
                    </div>
                    <div style='padding: 30px; color: #333333;'>
                        <h2 style='color: #d9534f;'>{headline}</h2>
                        <p>{introText}</p>
                        <div style='background-color: #fff5f5; border-right: 4px solid #d9534f; padding: 15px; margin: 20px 0; border-radius: 5px;'>
                            <p style='margin: 5px 0; color: #b71c1c;'>⚠️ <strong>إشعار الإدارة:</strong> يرجى مراجعة وتأكيد صحة الوثائق المرفوعة أو وضوح صورة سند الدفع، ثم إعادة إرسال الطلب عبر لوحة التحكم.</p>
                        </div>
                        <p>إذا كان لديكم أي استفسار، يسعدنا تواصلكم مع الدعم الفني للمنصة.</p>
                        <p style='margin-top: 30px; color: #666; font-size: 13px;'>شكراً لكم، إدارة منصة درب.</p>
                    </div>
                    <div style='background-color: #f0f0f0; padding: 15px; text-align: center;'>
                        <p style='margin: 0; font-size: 12px; color: #999;'>هذا البريد تم إرساله تلقائياً، يرجى عدم الرد عليه.</p>
                    </div>
                </div>";
        }
    }
}