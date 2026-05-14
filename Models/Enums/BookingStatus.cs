using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Models.Enums
{
    public enum BookingStatus
    {
        [Display(Name = "قيد الانتظار (بانتظار رفع السند)")]
        PendingAttachment = 0,

        [Display(Name = "في انتظار التأكيد")]
        AwaitingConfirmation = 1,

        [Display(Name = "مؤكد")]
        Confirmed = 2,

        [Display(Name = "ملغي")]
        Cancelled = 3,

        [Display(Name = "مكتمل")]
        Completed = 4
    }
}
