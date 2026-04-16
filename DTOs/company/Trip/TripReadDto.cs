namespace Darb.Api.DTOs.Trip
{
    public class TripReadDto
    {
        public int TripId { get; set; }
        public string StartGoveName { get; set; } = string.Empty;
        public string EndGoveName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime DepartureDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int AvailableSeats { get; set; }
        public int BusId { get; set; }
    }
}
