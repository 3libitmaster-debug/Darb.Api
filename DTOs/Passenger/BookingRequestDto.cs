using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Darb.Api.DTOs.Passenger
{
    public class BookingRequestDto
    {
        [Required]
        public int TripRouteId { get; set; }

        [Required]
        public int BankAccountId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one passenger is required.")]
        [MaxLength(10, ErrorMessage = "You cannot book more than 10 seats.")]

        public List<PassengerDetailDto> PassengerDetails { get; set; } = new List<PassengerDetailDto>();
    }
}
