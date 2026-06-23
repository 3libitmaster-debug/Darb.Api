namespace Darb.Api.DTOs.customer
{
    public class CompanyReviewResponseDto
    {
        public int ReviewId { get; set; }
        public string? CustomerName { get; set; }
        public int Rating { get; set; }
        public string? Description { get; set; }
        public string? Date { get; set; }
    }
}
