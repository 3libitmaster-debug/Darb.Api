using Darb.Api.Enums;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;



namespace Darb.Api.DTOs.AuthDtos
{
    public class RegisterCompanyDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(6)]
        public string Password { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(255)]
        public string Address { get; set; } = null!;

        [Required]
        public IFormFile Logo { get; set; } = null!;

        [Required]
        public IFormFile License { get; set; } = null!;

        [Required]
        public SubscriptionPlans PlanType { get; set; }

        [Required]
        public IFormFile? PaymentSlip { get; set; }
    }
}

