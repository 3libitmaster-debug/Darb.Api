using Darb.Api.Enums;
using Darb.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.company
{
    public class SubscriptionRenewalDto
    {
        [Required(ErrorMessage = "Plan type is required.")]
        public SubscriptionPlans PlanType { get; set; }

        [Required(ErrorMessage = "Payment slip is required.")]
        public IFormFile PaymentSlip { get; set; } = null!;
    }
}
