namespace Darb.Api.DTOs.Booking
{
    public class BookingPassengerReadDto
    {
        public int PassengerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}
