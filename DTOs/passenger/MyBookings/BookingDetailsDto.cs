namespace Darb.Api.DTOs.Booking
{
    public class BookingDetailsDto
    {
        public int BookingId { get; set; }
        public string BookingDate { get; set; } = string.Empty;
        public int ReservedSeatsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int StatusId { get; set; }

        
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public string StartGovernorate { get; set; } = string.Empty;
        public string EndGovernorate { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;

        // بيانات التذكرة
        public string? TicketCode { get; set; }
        public string? TicketStatus { get; set; }

        // قائمة الركاب (الآن متوافقة مع PassengerDetails)
        public List<PassengerItemDto> Passengers { get; set; } = new();
    }

    public class PassengerItemDto
    {
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}