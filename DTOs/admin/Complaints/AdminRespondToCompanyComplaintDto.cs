using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.admin.Complaints
{
    /// <summary>
    /// DTO لرد الأدمن على شكوى موجهة ضد شركة معينة.
    /// يتضمن عنوان ورسالة الإشعار الذي سيُرسل للشركة المعنية.
    /// </summary>
    public class AdminRespondToCompanyComplaintDto
    {
        /// <summary>عنوان الإشعار الذي سيُرسل إلى الشركة المعنية بالشكوى</summary>
        [Required(ErrorMessage = "عنوان إشعار الشركة مطلوب.")]
        [MaxLength(150, ErrorMessage = "عنوان الإشعار لا يتجاوز 150 حرف.")]
        public string CompanyNotificationTitle { get; set; } = string.Empty;

        /// <summary>نص/وصف الإشعار الذي سيُرسل إلى الشركة المعنية بالشكوى</summary>
        [Required(ErrorMessage = "نص إشعار الشركة مطلوب.")]
        [MaxLength(500, ErrorMessage = "نص الإشعار لا يتجاوز 500 حرف.")]
        public string CompanyNotificationBody { get; set; } = string.Empty;
    }
}
