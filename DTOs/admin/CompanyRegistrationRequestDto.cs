using System;

namespace Darb.Api.DTOs.Admin
{
    public class CompanyRegistrationRequestDto
    {
        public int CompanyId { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Logo { get; set; }
        public string? License { get; set; }
        public string? Email { get; set; }
        public DateTime RequestDate { get; set; }
        
        // Subscription Details
        public int SubscriptionId { get; set; }
        public string? PlanType { get; set; }
        public string? PaymentSlipUrl { get; set; }
    }
}
