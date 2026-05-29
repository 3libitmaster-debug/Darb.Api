using System;
using System.Collections.Generic;

namespace Darb.Api.DTOs.Booking
{
    public class CompanyScannedBookingDto
    {
        public int BookingId { get; set; }

        // Trip details
        public int TripId { get; set; }
        public int TripRouteId { get; set; }
        public string StartGovernorate { get; set; } = string.Empty;
        public string EndGovernorate { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public string DepartureTime { get; set; } = string.Empty;

        // Passengers
        public List<BookingPassengerReadDto> Passengers { get; set; } = new List<BookingPassengerReadDto>();
    }
}
