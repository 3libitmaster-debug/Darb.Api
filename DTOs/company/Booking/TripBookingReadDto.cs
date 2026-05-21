using System;

namespace Darb.Api.DTOs.Booking
{
    public class TripBookingReadDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ReservedSeatsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string StationName { get; set; } = string.Empty;
        public DateTime BookingAt { get; set; }
        public string? ReceiptImagePath { get; set; }
    }
}
