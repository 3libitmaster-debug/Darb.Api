using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.auth
{
    public class ForgetPasswordDto
    {
        [Required(ErrorMessage = "«·»—Ìœ «·≈·ﬂ —Ê‰Ì „ÿ·Ê».")]
        [EmailAddress(ErrorMessage = "’Ì€… «·»—Ìœ «·≈·ﬂ —Ê‰Ì €Ì— ’ÕÌÕ….")]
        public string Email { get; set; } = string.Empty;
    }
}
