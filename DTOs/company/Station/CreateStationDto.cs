using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Station
{
    public class CreateStationDto
    {

        [Required, StringLength(250)]
        public string ?Address { get; set; }

        public int CityId { get; set; }
        public int GovernorateId { get; set; }
    }
}
