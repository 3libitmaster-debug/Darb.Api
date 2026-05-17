using System.ComponentModel.DataAnnotations;
using Darb.Api.Models.Enums;

namespace Darb.Api.DTOs.Notification
{
    /// <summary>
    /// Data transfer object used for registering or updating an FCM device token.
    /// The UserId is omitted here because it is securely extracted from the authenticated JWT token.
    /// </summary>
    public class RegisterTokenDto
    {
        [Required(ErrorMessage = "Token is required.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Device Type is required.")]
        public DevicePlatform DeviceType { get; set; }
    }

    /// <summary>
    /// Data transfer object used for sending a notification.
    /// SenderType and SenderCompanyId are made optional here as they are securely resolved from the authenticated JWT token.
    /// </summary>
    public class SendNotificationDto
    {
        [Required(ErrorMessage = "Receiver ID is required.")]
        public int ReceiverId { get; set; }

        [Required(ErrorMessage = "Title is required."), MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Body is required."), MaxLength(500)]
        public string Body { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        public NotificationCategory Category { get; set; }

        public SenderRole? SenderType { get; set; }

        public int? SenderCompanyId { get; set; }
    }
}
