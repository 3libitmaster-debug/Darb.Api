using Darb.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.adminDtos.Advertisement
{
    public class AdvertisementCreateDto
    {

        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public IFormFile? ImageFile { get; set; }

        public DateTime? StartDateAds { get; set; }

        public DateTime? EndDateAds { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public AdsStatus AdsStatus { get; set; }
    }
}
