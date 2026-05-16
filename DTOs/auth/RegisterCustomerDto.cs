using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.AuthDtos
{
    public class RegisterPassengerDto
    {

        [Required(ErrorMessage = "ÇáÈÑíÏ ÇáÅáßÊÑæäí ãØáæÈ")]
        [EmailAddress(ErrorMessage = "ÕíÛÉ ÇáÈÑíÏ ÇáÅáßÊÑæäí ÛíÑ ÕÍíÍÉ")]
        public string ?Email { get; set; }

        [Required]
        [MinLength(8,ErrorMessage ="ßáãÉ ÇáãÑæÑ ÖÚíİÉ ÌÏÇğ!")]
        public string ?Password { get; set; }


        [Required(ErrorMessage = "ÇÓã ÇáÚãíá ãØáæÈ!")]
        [RegularExpression(@"^[a-zA-Z\u0600-\u06FF\s]+$", ErrorMessage = "ÇáÇÓã íÌÈ Ãä íÍÊæí Úáì ÍÑæİ İŞØ")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "ÇáÇÓã ŞÕíÑ ÌÏÇğ")]
        public string? FullName { get; set; }

        [Required(ErrorMessage ="ÊÇÑíÎ ÇáãíáÇÏ ãØáæÈ!")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "!ÑŞã ÇáåÇÊİ ãØáæÈ")]
        [RegularExpression(@"^(77|70|73|71|78)\d{7}$", ErrorMessage = "ÑŞã ÇáåÇÊİ ÛíÑ ÕÍíÍ")]
        public string ?Phone { get; set; }

        [Required, MinLength(11)]
        public string? NationalId { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; } = string.Empty;
    }
}
