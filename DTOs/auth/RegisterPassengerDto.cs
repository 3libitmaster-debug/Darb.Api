using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.AuthDtos
{
    public class RegisterPassengerDto
    {

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string ?Email { get; set; }

        [Required]
        [MinLength(8,ErrorMessage ="كلمة المرور ضعيفة جداً!")]
        public string ?Password { get; set; }


        [Required(ErrorMessage = "اسم العميل مطلوب!")]
        [RegularExpression(@"^[a-zA-Z\u0600-\u06FF\s]+$", ErrorMessage = "الاسم يجب أن يحتوي على حروف فقط")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم قصير جداً")]
        public string? FullName { get; set; }

        [Required(ErrorMessage ="تاريخ الميلاد مطلوب!")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "!رقم الهاتف مطلوب")]
        [RegularExpression(@"^(77|70|73|71|78)\d{7}$", ErrorMessage = "رقم الهاتف غير صحيح")]
        public string ?Phone { get; set; }

        [Required, MinLength(11)]
        public string? NationalId { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; } = string.Empty;
    }
}
