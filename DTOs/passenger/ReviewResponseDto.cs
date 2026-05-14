namespace Darb.Api.DTOs.passenger
{
    public class ReviewResponseDto
    {
        public int ReviewId { get; set; }
        public int Rating { get; set; }
        public string? Description { get; set; }
        public string? Date { get; set; }
    }
}
