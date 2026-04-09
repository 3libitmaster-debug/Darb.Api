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
        public string DepartureTime { get; set; } = string.Empty;

        [Required]
        public decimal SeatFare { get; set; }
    }
}
