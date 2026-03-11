using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Station
{
    public class UpdateStationDto
    {

        [StringLength(250)]
        public string? Address { get; set; }

        public int? Order { get; set; }

        public TimeSpan? DurationToEndStation { get; set; }

        public double? ExtraFee { get; set; }

        public int? CityId { get; set; }
        public int? GovernorateId { get; set; }
    }
}
