using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Darb.Api.Models.Enums;

namespace Darb.Api.Models
{


  public enum TripStatus
  {
    [Display(Name = "مجدولة")]
    scheduled = 0,

    [Display(Name = "ملغاة")]
    cancelled = 1,

    [Display(Name = "مكتملة")]
    completed = 2,

    [Display(Name = "ممتلئة")]
    Fulled = 3
  }


  public class Trip
  {
    public int TripId { get; set; }


    [Required]
    public int StartGoveId { get; set; }

    [ForeignKey("StartGoveId")]
    public virtual Governorate? StartGovernate { get; set; }

    [Required]
    public int EndGoveId { get; set; }

    [ForeignKey("EndGoveId")]
    public virtual Governorate? EndGovernate { get; set; }

    [Required]
    public int AvailableSeats { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]

    public DateTime DepDate { get; set; }

    public Periods Period { get; set; }

    public TripStatus TripStatus { get; set; } = TripStatus.scheduled;


    public int CompanyId { get; set; }
    [Required]
    public virtual Company? Company { get; set; }


    public int BusId { get; set; }
    public virtual Bus? Bus { get; set; }

    public virtual ICollection<TripRoute>? TripRoutes { get; set; }




  }
}
