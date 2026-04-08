namespace Darb.Api.DTOs.Passenger
{
    public class TripRouteResponseDto
    {
        public int TripRouteId { get; set; }
        public int TripId { get; set; }
        public int StationId { get; set; }
        public string DepartureTime { get; set; } = string.Empty;
        public decimal RouteFare { get; set; }
        public string? CityName { get; set; }
        public string? Address { get; set; }

    }
}
