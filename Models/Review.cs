using Darb.Api.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int PassengerId { get; set; }
        
        [ForeignKey(nameof(PassengerId))]
        public virtual Passenger? Passenger { get; set; }

        [Required]
        public int CompanyId { get; set; }
        
        [ForeignKey(nameof(CompanyId))]
        public virtual Company? Company { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "التقييم يجب أن يكون بين 1 و 5 نجوم")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "الوصف لا يمكن أن يتجاوز 1000 حرف")]
        public string? Description { get; set; }

        [Required]
        public DateTime ReviewDate { get; set; } = DateHelper.GetYemenTime();

        

    
    }
}