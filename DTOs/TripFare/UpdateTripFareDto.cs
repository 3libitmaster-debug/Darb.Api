using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.TripFare
{
    public class UpdateTripFareDto
    {
        public decimal? Price { get; set; }
        public int? MinutesOffset { get; set; }
        public bool IsMainStation { get; set; }
    }

       
}
