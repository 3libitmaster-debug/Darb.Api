namespace Darb.Api.DTOs.passengerDtos.bookingDtos
{
    public class TripScheduleResponseDto
    {
        public int TripScheduleId { get; set; }
        public int TripId { get; set; }
        public int StationId { get; set; }
        public string ?DepartureTime { get; set; } 
        public string? CityName { get; set; }
        public string? Address { get; set; }
        public decimal SeatFare { get; set; }


    }
}
