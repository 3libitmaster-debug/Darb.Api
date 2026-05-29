using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.auth
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "«·»—Ìœ «·≈·ﬂ —Ê‰Ì „ÿ·Ê».")]
        [EmailAddress(ErrorMessage = "’Ì€… «·»—Ìœ «·≈·ﬂ —Ê‰Ì €Ì— ’ÕÌÕ….")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "—„“ «· Õﬁﬁ „ÿ·Ê».")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "ﬂ·„… «·„—Ê— «·ÃœÌœ… „ÿ·Ê»….")]
        [MinLength(6, ErrorMessage = "ÌÃ» √·«  ﬁ· ﬂ·„… «·„—Ê— ⁄‰ 6 √Õ—›.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
