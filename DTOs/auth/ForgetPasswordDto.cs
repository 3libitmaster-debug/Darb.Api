using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.auth
{
    public class ForgetPasswordDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        public string Email { get; set; } = string.Empty;
    }
}
