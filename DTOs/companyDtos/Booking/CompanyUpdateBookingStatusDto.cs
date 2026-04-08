using Darb.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Booking
{
    public class CompanyUpdateBookingStatusDto
    {
        [Required]
        public BookingStatus Status { get; set; }
    }
}
