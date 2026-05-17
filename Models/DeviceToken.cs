using Darb.Api.Helpers;
using Darb.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{

    public class DeviceToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } 

        [Required]
        public DevicePlatform? DeviceType { get; set; } 

        public DateTime CreatedAt { get; set; } = DateHelper.GetYemenTime(); 
        public DateTime LastUpdatedAt { get; set; } = DateHelper.GetYemenTime();

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User ?User { get; set; }
    }
}
