using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.admin.Customers
{
    public class CustomerCreateDto
    {
        [Required, EmailAddress] public string Email { get; set; } = null!;
        [Required, MinLength(6)] public string Password { get; set; } = null!;
        [Required, MaxLength(255)] public string FullName { get; set; } = null!;
        [Required] public DateTime DateOfBirth { get; set; }
        [Required, Phone] public string Phone { get; set; } = null!;
        [Required, MaxLength(255)] public string Address { get; set; } = null!;
        [Required, MaxLength(11)] public string NationalId { get; set; } = null!;
    }
}
