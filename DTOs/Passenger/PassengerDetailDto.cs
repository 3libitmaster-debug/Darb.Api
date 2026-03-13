using System.ComponentModel.DataAnnotations;
using Darb.Api.Models;

namespace Darb.Api.DTOs.Passenger
{
    public class PassengerDetailDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "National ID is required.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "National ID must contain only digits.")]
        [StringLength(11, ErrorMessage = "National ID cannot exceed 11 numbers.")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? BirthDate { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public Gender Gender { get; set; }
    }
}
