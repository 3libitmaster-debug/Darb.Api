using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Models
{
    public class City
    {
        public int CityId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int GovernorateId { get; set; }
        public Governorate? Governorate { get; set; }
    }
}
