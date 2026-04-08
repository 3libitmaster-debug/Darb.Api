namespace Darb.Api.DTOs.adminDtos.Advertisement
{
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

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
