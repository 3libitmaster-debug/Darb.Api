using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Station
{
    public class UpdateStationDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        public int? Order { get; set; }

        public TimeSpan? DurationFromStart { get; set; }

        public double? ExtraFee { get; set; }

        public int? CityId { get; set; }
        public int? GovernorateId { get; set; }
    }
}
