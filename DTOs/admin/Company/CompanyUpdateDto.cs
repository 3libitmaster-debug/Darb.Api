using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.admin.Company
{
    public class CompanyUpdateDto
    {
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        public string? Email { get; set; }

        public string? Password { get; set; }

        [MaxLength(150, ErrorMessage = "اسم الشركة لا يجب أن يتجاوز 150 حرفاً.")]
        public string? Name { get; set; }

        [MaxLength(255, ErrorMessage = "العنوان لا يجب أن يتجاوز 255 حرفاً.")]
        public string? Address { get; set; }

        public IFormFile? LogoFile { get; set; } 

        public string? License { get; set; }

        public bool? IsActive { get; set; }
    }
}
