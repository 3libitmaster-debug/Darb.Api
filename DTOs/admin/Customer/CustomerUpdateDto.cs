using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.admin.Customers
{
    public class CustomerUpdateDto
    {
        [EmailAddress] public string? Email { get; set; }
        public string? Password { get; set; }
        [MaxLength(255)] public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [Phone] public string? Phone { get; set; }
        [MaxLength(255)] public string? Address { get; set; }
        [MaxLength(11)] public string? NationalId { get; set; }

    }
}
