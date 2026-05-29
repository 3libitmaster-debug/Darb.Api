using Darb.Api.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.company
{
    public class SubscriptionRenewalDto
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subscription plan type is required.")]
        public SubscriptionPlans PlanType { get; set; }

        [Required(ErrorMessage = "Payment slip image file is required.")]
        public IFormFile? PaymentSlip { get; set; }
    }
}