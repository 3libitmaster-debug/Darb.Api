using System;
using System.Collections.Generic;

namespace Darb.Api.DTOs.passengerDtos.bookingDtos
{
    public class MyBookingDto
    {
        public int BookingId { get; set; }
        public int BookingStatus { get; set; } 
        public DateTime BookingAt { get; set; }
        public decimal TotalAmount { get; set; }
        public int NumberOfSeats { get; set; }

        public int TripScheduleId { get; set; }
        public int TripId { get; set; }
        public string StartGovernorate { get; set; } = string.Empty;
        public string EndGovernorate { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;

        public List<MyTicketDto> Tickets { get; set; } = new List<MyTicketDto>();
    }

    public class MyTicketDto
    {
        public int ETicketId { get; set; }
        public string? TicketCode { get; set; }
        public int TicketStatus { get; set; } 
        public string PassengerName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
    }
}
