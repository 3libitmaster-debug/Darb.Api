using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Darb.Api.Models
{
    public class TripSchedule
    {
        [Key]
        public int TripScheduleId { get; set; }

        [Required]
        public int TripId { get; set; }
        [ForeignKey("TripId")]
        public virtual Trip ?Trip { get; set; }

        [Required]
        public int StationId { get; set; }
        [ForeignKey("StationId")]
        public virtual Station ?Station { get; set; }

        [Required]
        public TimeOnly DepartureTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SeatFare { get; set; }

    }
}
