namespace Darb.Api.DTOs.customer.Complaints
{
    /// <summary>
    /// بيانات الشكوى المعروضة للعميل (شكاواه الخاصة فقط)
    /// </summary>
    public class CustomerComplaintResponseDto
    {
        public int ComplaintId { get; set; }
        public string? ComplaintType { get; set; }
        public string? CompanyName { get; set; }    // null إذا كانت الشكوى دعماً فنياً
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AdminResponse { get; set; }  // null حتى يرد الأدمن
    }
}
