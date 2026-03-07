using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class TripFare
    {
        public int TripFareId { get; set; }

        [ForeignKey("TripId")]
        public int TripId { get; set; }
        [Required]
        public virtual Trip ?Trip { get; set; }

        [ForeignKey("StationId")]
        public int StationId { get; set; }
        [Required]
        public virtual Station? Station { get; set; }

        [Required]
        public double ActualPrice { get; set; }

        [Required]
        public TimeSpan ActualDepartureTime { get; set; }
    


    }
}
