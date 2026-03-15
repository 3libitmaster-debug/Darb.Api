using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Passenger
{
    public class BookingRequestDto
    {
        [Required(ErrorMessage = "„”«— «·—Õ·… „ÿ·Ê».")]
        public int TripRouteId { get; set; }

        public bool IsOwnerPassenger { get; set; }

        [Required(ErrorMessage = "·« Ì„ﬂ‰ «·ÕÃ“ »œÊ‰  ›«’Ì· «·—ﬂ«»!.")]
        public List<PassengerDetailDto> AdditionalPassengers { get; set; } = new List<PassengerDetailDto>();
    }
}