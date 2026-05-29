using System.Collections.Generic;

namespace Darb.Api.DTOs.Booking
{
    public class CompanyBookingDetailsDto : CompanyBookingReadDto
    {
        public string? TicketCode { get; set; }
        public List<CompanyPassengerDetailDto> Customers { get; set; } = new List<CompanyPassengerDetailDto>();
    }

    public class CompanyPassengerDetailDto
    {
        public int PassengerDetailId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
    }
}
