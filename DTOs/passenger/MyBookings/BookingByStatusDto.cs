namespace Darb.Api.DTOs.passenger.MyBookings
{
    public class BookingByStatusDto
    {
        public int BookingId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public string StartGovernorate { get; set; } = string.Empty;
        public string EndGovernorate { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
