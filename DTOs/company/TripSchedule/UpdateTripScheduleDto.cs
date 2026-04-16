namespace Darb.Api.DTOs.TripSchedule
{
    public class UpdateTripScheduleDto
    {
        public TimeOnly? DepartureTime { get; set; }
        public decimal? SeatFare { get; set; }
    }
}
