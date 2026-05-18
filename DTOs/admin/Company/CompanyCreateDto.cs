using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.admin.Company
{
    public class CompanyCreateDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        [MinLength(6, ErrorMessage = "يجب ألا تقل كلمة المرور عن 6 خانات.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "اسم الشركة مطلوب.")]
        [MaxLength(150, ErrorMessage = "اسم الشركة لا يجب أن يتجاوز 150 حرفاً.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "عنوان الشركة الرئيسي مطلوب.")]
        [MaxLength(255, ErrorMessage = "العنوان لا يجب أن يتجاوز 255 حرفاً.")]
        public string Address { get; set; } = null!;

        [Required(ErrorMessage = "ملف شعار الشركة مطلوب.")]
        public IFormFile LogoFile { get; set; } = null!; // لاستقبال الشعار كملف فوري عبر الـ Form

        [Required(ErrorMessage = "رقم السجل التجاري أو الترخيص مطلوب.")]
        public string License { get; set; } = null!;

        public bool IsActive { get; set; } = true; // حالة الحساب الافتراضية عند الإنشاء
    }
}
