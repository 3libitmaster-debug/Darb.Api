using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class TripFare
    {
        [Key]
        public int TripFareId { get; set; }

        [Required]
        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [Required]
        public int FromGovId { get; set; }
        [ForeignKey("FromGovId")]
        public virtual Governorate? FromGovernorate { get; set; }

        [Required]
        public int ToGovId { get; set; }
        [ForeignKey("ToGovId")]
        public virtual Governorate? ToGovernorate { get; set; }

        [Required]
        public int StationId { get; set; }
        [ForeignKey("StationId")]
        public virtual Station? Station { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required]
        public int MinutesOffset { get; set; }
    }
}
