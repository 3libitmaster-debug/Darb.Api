using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.company.TripRoute
{
    public class AddTripRouteDto
    {
        [Required]
        public int StationId { get; set; }

        [Required]
        public TimeOnly? DepartureTime { get; set; }
    }
}
