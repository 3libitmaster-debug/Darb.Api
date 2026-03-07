using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.City
{
    public class CityCreateDto 
    { 
        [Required]
        public string? Name { get; set; } 
        [Required] public int GovernorateId { get; set; }
    }

}
