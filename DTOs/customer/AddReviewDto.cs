using System.ComponentModel.DataAnnotations;
namespace Darb.Api.DTOs.passengerDtos.settings 
{
    public class AddReviewDto
    {
        public int CompanyId { get; set; }
        public int Rating { get; set; } // 1 to 5
        public string? Description { get; set; }
    }
}