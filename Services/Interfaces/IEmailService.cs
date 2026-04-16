namespace Darb.Api.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Generates a 6-digit OTP, sends it to the specified email, and returns the generated OTP.
        /// Returns null if the email fails to send.
        /// </summary>
        Task<string?> SendOtpEmailAsync(string to);
    }
}
