namespace Darb.Api.DTOs.admin.Complaints
{
   
    public class AdminComplaintResponseDto
    {
        public int ComplaintId { get; set; }

       
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }

        // --- تفاصيل الشكوى ---
        public string? ComplaintType { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AdminResponse { get; set; }
    }
}
