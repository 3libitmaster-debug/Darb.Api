using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.passengerDtos.bookingDtos
{
  public class BookingRequestDto
  {
    [Required(ErrorMessage = "اختيار المسار مطلوب.")]
    public int TripRouteId { get; set; }

    [Required(ErrorMessage = "يجب إضافة بيانات الركاب!.")]
    public List<PassengerDetailDto> AdditionalPassengers { get; set; } = new List<PassengerDetailDto>();
  }
}
