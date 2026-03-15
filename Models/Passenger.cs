using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class Passenger
    {
        [Key]
        public int PassengerId { get; set; }

        [Required,MaxLength(255)]
        public string ?FullName { get; set; }

        [Required,Column(TypeName = "date")]
        public DateTime DateOfBirth { get; set; }

        [Required,Phone]
        public string ?Phone { get; set; }

        [Required,MaxLength(255)]
        public string ?Address { get; set; }

        [Required, MaxLength(11)]
        public string ?NationalId { get; set; } 

        [ForeignKey("UserId")]
        public int UserId { get; set; }

        [Required]
        public User ?User { get; set; }


    }
}
