using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.auth
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "كلمة المرور السابقة مطلوبة")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل")]
        [MaxLength(100, ErrorMessage = "كلمة المرور الجديدة يجب ألا تتجاوز 100 حرف")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
