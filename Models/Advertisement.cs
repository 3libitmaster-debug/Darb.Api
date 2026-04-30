using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public enum AdsStatus
    {
        Active,
        Inactive,
        Expired
    }
    public class Advertisement
    {
        [Key]
        public int AdvertisementID { get; set; }

        [Required]
        public int AccountId { get; set; }

        [ForeignKey("AccountId")]
        public Account? Account { get; set; }

        [MaxLength(100)]
        public string? AdsTitle { get; set; }

        public string? Description { get; set; }

        public string? Image { get; set; }

        public DateTime? StartDateAds { get; set; }

        public DateTime? EndDateAds { get; set; }

        [Required]
        public AdsStatus AdsStatus { get; set; }

  

        [Required]
        public DateTime AdsCreatedAt { get; set; } = DateTime.UtcNow;
    }
}
