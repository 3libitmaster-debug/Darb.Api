using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.customer.Complaints
{
    /// <summary>
    /// DTO لتعديل شكوى موجودة (فقط العنوان والتفاصيل قابلة للتعديل)
    /// </summary>
    public class UpdateComplaintDto
    {
        [Required(ErrorMessage = "عنوان الشكوى مطلوب.")]
        [MaxLength(150, ErrorMessage = "العنوان لا يتجاوز 150 حرفاً.")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "تفاصيل الشكوى مطلوبة.")]
        [MaxLength(2000, ErrorMessage = "التفاصيل لا تتجاوز 2000 حرف.")]
        public string? Description { get; set; }
    }
}
