using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Enums
{
    public enum SubscriptionPlans
    {
        [Display(Name = "اشتراك شهري - 90 ريال سعودي")]
        Monthly = 0,

        [Display(Name = "اشتراك سنوي - 1050 ريال سعودي")]
        Yearly = 1
    }
}