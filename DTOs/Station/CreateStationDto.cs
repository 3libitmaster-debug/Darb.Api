using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Station
{
    public class CreateStationDto
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(250)]
        public string Address { get; set; }

        [Required]
        public int Order { get; set; }

        [Required]
        public TimeSpan DurationFromStart { get; set; }

        [Required]
        public double ExtraFee { get; set; }

        public int CityId { get; set; }
        public int GovernorateId { get; set; }
    }
}
