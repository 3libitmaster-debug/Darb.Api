using System;

namespace Darb.Api.DTOs.TripRoute
{
  public class TripRouteReadDto
  {
    public int TripRouteId { get; set; }
    public int TripId { get; set; }
    public int StationId { get; set; }
    public string? StationName { get; set; }
    public string? CityName { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public decimal SeatFare { get; set; }
  }
}
