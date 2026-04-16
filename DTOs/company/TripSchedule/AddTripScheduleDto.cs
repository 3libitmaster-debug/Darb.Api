using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.TripSchedule
{
    public class AddTripScheduleDto
    {
        [Required]
        public int TripId { get; set; }

        [Required]
        public int StationId { get; set; }

        [Required]
        public TimeOnly DepartureTime { get; set; }

        [Required]
        public decimal SeatFare { get; set; }
    }
}
