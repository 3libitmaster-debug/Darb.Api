namespace Darb.Api.DTOs.Passenger
{
    /// <summary>
    /// Represents the data displayed in the search results list for a passenger.
    /// </summary>
    public class TripSearchResultDto
    {
        public int TripId { get; set; }

        // Company Information
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;

        // Departure Info (Unified)
        public int StartGoveId { get; set; }
        public string StartGoveName { get; set; } = string.Empty;

        // Destination Info (Unified)
        public int EndGoveId { get; set; }
        public string EndGoveName { get; set; } = string.Empty;

        // Financial & Timing Info
        public decimal Price { get; set; }
        public string DepartureTime { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public int AvailableSeats { get; set; }
        public string Period { get; set; } = string.Empty;
    }
}