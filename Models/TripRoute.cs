using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Darb.Api.Models
{
    public class TripRoute
    {
        [Key]
        public int TripRouteId { get; set; }

        [Required]
        public int TripId { get; set; }

        [Required]
        public int StationId { get; set; }

        [Required]
        public TimeSpan DepartureTime { get; set; }

        [ForeignKey("TripId")]
        public virtual Trip ?Trip { get; set; }

        [ForeignKey("StationId")]
        public virtual Station ?Station { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal RouteFare { get; set; }

    }
}
