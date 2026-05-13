using System;
using System.Collections.Generic;

namespace Darb.Api.DTOs.passengerDtos.bookingDtos
{
    public class MyBookingDto
    {
        // booking info
        public int BookingId { get; set; }
        public int BookingStatus { get; set; } 
        public DateTime BookingAt { get; set; }
        public decimal TotalAmount { get; set; }
        public int ReservedSeatsCount { get; set; }


        public int TripScheduleId { get; set; }
        public string ?StationName {get;set;}
        public int TripId { get; set; }
        public string StartGovernorate { get; set; } = string.Empty;
        public string EndGovernorate { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;

        public MyTicketDto? Ticket { get; set; }
    }

   
}
