using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Darb.Api.Models
{
    public enum Gender
    {
        Male = 0, Female = 1,
    }
 
    public class PassengerDetails
    {

        public int PassengerDetailsId { get; set; }

        public int BookingId { get; set; }
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "National Number is required.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "National number must contain only digits.")]
        [StringLength(11, ErrorMessage = "National number cannot exceed 11 numbers.")]
        public string NationalNumber { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

   
    }
}