using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Models.Enums
{
    public enum BookingStatus
    {

        [Display(Name = "›Ì «‰ Ÿ«— «· √ﬂÌœ")]
        AwaitingConfirmation = 1,

        [Display(Name = "„ƒﬂœ")]
        Confirmed = 2,

        [Display(Name = "„·€Ì")]
        Cancelled = 3,

        [Display(Name = "„ﬂ „·")]
        Completed = 4,

        [Display(Name = "„—›Ê÷")]
        Rejected = 5,

        [Display(Name = "›Ì «‰ Ÿ«— «·«·€«¡")]
        AwaitingCancellation = 6


    }
}
