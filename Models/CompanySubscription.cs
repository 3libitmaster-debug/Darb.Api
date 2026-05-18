using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Darb.Api.Models.Enums;
using Darb.Api.Enums;

namespace Darb.Api.Models
{
    public enum SubscriptionStatus
    {
        Pending,   // ÊÍÊ ÇáãÑÇÌÚÉ ãä ÇáÅÏÇÑÉ
        Approved,  // Êã ŞÈæá ÇáÇÔÊÑÇß æÊİÚíáå
        Rejected   // Êã ÑİÖ ÇáØáÈ (ãËáÇğ ÇáÓäÏ ÛíÑ æÇÖÍ Ãæ ãÒæÑ)
    }

    public enum RequestType
    {
        NewRegistration, // ØáÈ ÇäÖãÇã ÌÏíÏ
        Renewal          // ØáÈ ÊÌÏíÏ ÇÔÊÑÇß
    }

    public class CompanySubscription
    {
        [Key]
        public int CompanySubscriptionId { get; set; }

        [Required]
        public SubscriptionPlans PlanType { get; set; }

        [Required]
        public DateTime SubscriptionDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public string PaymentSlip { get; set; } = null!;

        // ÇáÍŞæá ÇáÌÏíÏÉ áÊÓåíá ÇáãäØŞ ÇáÈíÑãÌí æİáÊÑÉ áæÍÉ ÇáÊÍßã
        [Required]
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;

        [Required]
        public RequestType RequestType { get; set; } // åäÇ íÊÍÏÏ (ÌÏíÏ Ãã ÊÌÏíÏ)

        public int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public virtual Company Company { get; set; } = null!;
    }
}
