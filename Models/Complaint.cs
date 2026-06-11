using Darb.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class Complaint
    {
        [Key]
        public int ComplaintId { get; set; }

        // --- العلاقة مع العميل ---
        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        // --- نوع الشكوى ---
        [Required]
        public ComplaintType ComplaintType { get; set; }

        // --- العلاقة مع الشركة (اختيارية - تُعبأ فقط عند نوع Company) ---
        public int? CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }

        // --- تفاصيل الشكوى ---
        [Required, MaxLength(150)]
        public string? Title { get; set; }

        [Required, MaxLength(2000)]
        public string? Description { get; set; }

        // --- الحالة (Pending افتراضياً) ---
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;

        // --- تاريخ الإنشاء ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- رد الأدمن (فارغ حتى يرد الأدمن) ---
        public string? AdminResponse { get; set; }
    }
}
