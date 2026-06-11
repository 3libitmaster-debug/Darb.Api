using Darb.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.admin.Complaints
{
    /// <summary>
    /// DTO لرد الأدمن على شكوى وتحديث حالتها
    /// </summary>
    public class AdminRespondToComplaintDto
    {
        [MaxLength(2000, ErrorMessage = "الرد لا يتجاوز 2000 حرف.")]
        public string? AdminResponse { get; set; }

        [Required(ErrorMessage = "حالة الشكوى مطلوبة.")]
        public ComplaintStatus Status { get; set; }
    }
}
