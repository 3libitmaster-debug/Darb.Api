using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Advertisement
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
        public bool Status { get; set; } = true;
    }

    public class AdvertisementUpdateDto
    {
        public int? UserID { get; set; }

        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public IFormFile? ImageFile { get; set; }

        public DateTime? StartDateAds { get; set; }

        public DateTime? EndDateAds { get; set; }

        public bool? Status { get; set; }
    }

    public class AdvertisementReadDto
    {
        public int AdvertisementID { get; set; }

        public int UserID { get; set; }

        public string? User_Email { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime? StartDateAds { get; set; }

        public DateTime? EndDateAds { get; set; }

        public bool Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
