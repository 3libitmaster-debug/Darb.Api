namespace Darb.Api.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Generates a 6-digit OTP, sends it to the specified email, and returns the generated OTP.
        /// Returns null if the email fails to send.
        /// </summary>
        Task<string?> SendOtpEmailAsync(string to);

        /// <summary>
        /// Sends an approval notification email to the company.
        /// isRenewal=true means it's a renewal, false means it's a new registration.
        /// </summary>
        Task<bool> SendSubscriptionApprovalEmailAsync(string to, string companyName, bool isRenewal, DateTime expiryDate);

        Task<bool> SendSubscriptionRejectionEmailAsync(string to, string companyName, bool isRenewal);
    }
}

