using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.auth
{
    public class SendOtpDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
