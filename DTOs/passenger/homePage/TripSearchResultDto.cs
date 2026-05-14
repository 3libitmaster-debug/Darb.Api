namespace Darb.Api.DTOs.passengerDtos.homePageDtos
{
    /// <summary>
    /// Represents the data displayed in the search results list for a passenger.
    /// </summary>
    public class TripSearchResultDto
    {
        public int TripId { get; set; }

        // Company Information
        public int CompanyId { get; set; }
        public string ?CompanyName { get; set; }
        public string ?CompanyLogo { get; set; } 
        public double CompanyRating { get; set; }

        // Departure Info (Unified)
        public int StartGoveId { get; set; }
        public string ?StartGoveName { get; set; } 

        // Destination Info (Unified)
        public int EndGoveId { get; set; }
        public string ?EndGoveName { get; set; }

        // Financial & Timing Info
        public decimal BasePrice { get; set; }
        public string ?DepartureDate { get; set; }
        public int AvailableSeats { get; set; }
        public string ?Period { get; set; } 
    }
}