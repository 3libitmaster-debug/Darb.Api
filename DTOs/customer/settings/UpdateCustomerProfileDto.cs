using System;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.passengerDtos.settings
{
    public class UpdateCustomerProfileDto
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MaxLength(255, ErrorMessage = "الاسم الكامل لا يجب أن يتجاوز 255 حرفاً")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "العنوان مطلوب")]
        [MaxLength(255, ErrorMessage = "العنوان لا يجب أن يتجاوز 255 حرفاً")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [MaxLength(11, ErrorMessage = "الرقم الوطني يجب أن يتكون من 11 حرفاً كحد أقصى")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
        public string Email { get; set; } = string.Empty;
    }
}
