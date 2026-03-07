using System.ComponentModel.DataAnnotations;
namespace Darb.Api.Models.Enums
{
    public enum Periods
    {
        [Display(Name = "صباحي")]
        Day = 0,

        [Display(Name = "مسائي")]
        Night = 1
    }
}