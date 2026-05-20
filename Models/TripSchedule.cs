using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Darb.Api.Models
{
  public class TripRoute
  {
    [Key]
    public int TripRouteId { get; set; }

    [Required]
    public int TripId { get; set; }
    [ForeignKey("TripId")]
    public virtual Trip? Trip { get; set; }

    [Required]
    public int StationId { get; set; }
    [ForeignKey("StationId")]
    public virtual Station? Station { get; set; }

    [Required]
    public TimeOnly DepartureTime { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal SeatFare { get; set; }

  }
}
