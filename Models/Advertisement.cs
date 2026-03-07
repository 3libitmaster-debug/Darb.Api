using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class Advertisement
    {
        [Key]
        public int AdvertisementID { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [MaxLength(100)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Image { get; set; }

        public DateTime? StartDateAds { get; set; }

        public DateTime? EndDateAds { get; set; }

        [Required]
        public bool IsActive { get; set; }

  

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
