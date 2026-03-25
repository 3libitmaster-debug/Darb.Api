using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.TripFare
{
    public class UpdateTripFareDto
    {
        public decimal? Price { get; set; }
        public int? MinutesOffset { get; set; }
        // Station/Governorate IDs typically aren't updated. If a trip fare changes route completely, they probably delete and recreate.
    }
}
