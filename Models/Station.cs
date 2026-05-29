using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class Station
    {
     
        public int StationId { get; set; }

        [Required,StringLength(250)]
        public string? Address { get; set; }
    
        public int CityId { get; set; }
        [ForeignKey("CityId")]
        public virtual City? City { get; set; }

        public int GovernorateId { get; set; }
        [ForeignKey("GovernorateId")]
        public virtual Governorate? Governorate { get; set; }

        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        }
}