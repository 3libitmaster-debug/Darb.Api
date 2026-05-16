using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Darb.Api.Models.Enums;

namespace Darb.Api.Models
{
   
    public class CompanySubscription
    {

        public int CompanySubscriptionId { get; set; }

        [Required]
        public SubscriptionPlans PlanType { get; set; }

        [Required,]
        public DateTime SubscriptionDate { get; set; }


        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public string ? PaymentSlip { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [ForeignKey("CompanyId")]
        public Company? Company{ get; set; }


    }
}
