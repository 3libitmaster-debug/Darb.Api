using Darb.Api.Models;
using Darb.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Booking
{
    public class CompanyUpdateBookingStatusDto
    {
        [Required]
        public BookingStatus Status { get; set; }
    }
}
