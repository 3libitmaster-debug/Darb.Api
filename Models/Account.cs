using System.ComponentModel.DataAnnotations;
using Darb.Api.Models.Enums;
namespace Darb.Api.Models
{


    public class Account
    {
        public int AccountId { get; set; }


        public string? Email { get; set; }

        [Required, MaxLength(100)]
        public string? Password { get; set; }

        [Required]
        public AccountRoles Role { get; set; }

        [Required]
        public bool IsActive { get; set; } = false;

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        public Passenger? Passenger { get; set; }

        public Company? Company { get; set; }

    }
}
