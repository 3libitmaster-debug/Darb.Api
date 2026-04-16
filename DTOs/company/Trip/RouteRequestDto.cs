using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.companyDtos.Trip
{
    public class RouteRequestDto
    {
        [Required]
        public int StationId { get; set; }

        [Required]
        public TimeOnly? DepartureTime { get; set; }
    }
}
