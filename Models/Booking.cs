using Darb.Api.Helpers;
using Darb.Api.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{


  public class Booking
  {
    [Key]
    public int BookingId { get; set; }


    // --- Foreign Keys ---

    [Required]
    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customers { get; set; }

    [Required]
    public int TripRouteId { get; set; }

    [ForeignKey("TripRouteId")]
    public virtual TripRoute? TripRoute { get; set; }

    public virtual ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();

    public int ReservedSeatsCount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? ReceiptImagePath { get; set; }

    [Required]
    public BookingStatus Status { get; set; }

    public DateTime BookingAt { get; set; }

    public virtual ETicket? ETicket { get; set; }
  }


}
