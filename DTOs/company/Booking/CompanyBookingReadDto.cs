using Darb.Api.Models;

namespace Darb.Api.DTOs.Booking
{
  public class CompanyBookingReadDto
  {
    public int BookingId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public int TripId { get; set; }

    public int TripRouteId { get; set; }
    public string StartGovernorate { get; set; } = string.Empty;
    public string EndGovernorate { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public int ReservedSeatsCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ReceiptImagePath { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime BookingAt { get; set; }
  }
}
