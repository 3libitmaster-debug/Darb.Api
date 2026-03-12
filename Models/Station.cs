using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class Station
    {
     
        public int StationId { get; set; }

        [Required,StringLength(250)]
        public string? Address { get; set; }

        // ترتيب المحطة داخل المحافظة انطلاق الرحلة بحيث اول محطة داخل المحافظة انطلاق
        // ... الرحلة تأخذ الترتيب 1 وثاني محطة تأخذ الترتيب 2 وهكذا
        [Required]
        public int Order { get; set; }

        // المدة الزمنية التي تستغرق للانتقال من المحطة المختارة الى اخر محطة داخل محافظة أنطلاق الرحلة
        [Required]
        public TimeSpan DurationToEndStation { get; set; }

        // التكاليف الاضافية التي تضاف على سعر الرحلة في حال كانت المحطة المختارة ليست اخر محطة داخل محافظة أنطلاق الرحلة
        [Required]
        public decimal ExtraFee { get; set; }
    
        public int CityId { get; set; }
        [ForeignKey("CityId")]
        public virtual City? City { get; set; }

        public int GovernorateId { get; set; }
        [ForeignKey("GovernorateId")]
        public virtual Governorate? Governorate { get; set; }

        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        }
}