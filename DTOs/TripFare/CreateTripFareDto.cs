using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.TripFare
{
    public class CreateTripFareDto
    {
        [Required]
        public int FromGovId { get; set; }

        [Required]
        public int ToGovId { get; set; }

        [Required]
        public int StationId { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int MinutesOffset { get; set; }
    }
}
