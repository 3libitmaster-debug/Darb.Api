using Darb.Api.Enums;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using System;

namespace Darb.Api.DTOs.Admin
{
    public class PendingSubscriptionDto
    {
        public int CompanySubscriptionId { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public SubscriptionPlans PlanType { get; set; }
        public DateTime SubscriptionDate { get; set; }
        public string? PaymentSlipUrl { get; set; }
        public RequestType RequestType { get; set; }
    }
}
