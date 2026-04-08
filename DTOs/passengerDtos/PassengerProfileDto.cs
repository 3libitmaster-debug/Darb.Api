using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Passenger
{
    public class PassengerProfileDto
    {
        public int PassengerId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string NationalId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
