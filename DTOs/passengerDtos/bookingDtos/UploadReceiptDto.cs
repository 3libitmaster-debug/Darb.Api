using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.passengerDtos.bookingDtos
{
    public class UploadReceiptDto
    {
        [Required]
        public int BookingId { get; set; }
        
        [Required(ErrorMessage = "Receipt image is required.")]
        public IFormFile? ReceiptImage { get; set; }
    }
}
