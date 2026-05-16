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
        public int CustomerId { get; set; }
        
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [Required]
        public int CompanyId { get; set; }
        
        [ForeignKey(nameof(CompanyId))]
        public virtual Company? Company { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "«· ﬁÌÌ„ ÌÃ» √‰ ÌﬂÊ‰ »Ì‰ 1 Ê 5 ‰ÃÊ„")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "«·Ê’› ·« Ì„ﬂ‰ √‰ Ì Ã«Ê“ 1000 Õ—›")]
        public string? Description { get; set; }

        [Required]
        public DateTime ReviewDate { get; set; } = DateHelper.GetYemenTime();

        

    
    }
}