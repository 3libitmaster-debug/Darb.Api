using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.AuthDtos
{
    public class LoginDto
    {
        [Required]
        public string ?Email { get; set; } 

        [Required]
        public string ?Password { get; set; } 

    }
}
