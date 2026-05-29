namespace Darb.Api.DTOs.passengerDtos.bookingDtos
{
  public class TripRouteResponseDto
  {
    public int TripRouteId { get; set; }
    public int TripId { get; set; }
    public int StationId { get; set; }
    public string? DepartureTime { get; set; }
    public string? CityName { get; set; }
    public string? Address { get; set; }
    public decimal SeatFare { get; set; }


  }
}
