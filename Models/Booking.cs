using Darb.Api.Helpers;
using Darb.Api.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public enum BookingStatus
    {
        Pending = 0,    // Waiting for payment
        Confirmed = 1,  // Paid and seat reserved
        Cancelled = 2,  // Cancelled by user or admin

    }

    public class Booking
    {
        [Key]
        public int BookingId { get; set; }


        [Required(ErrorMessage = "Booking date is required.")]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        // --- Foreign Keys ---

        [Required]
        public int PassengerId { get; set; }
        [ForeignKey("PassengerId")]
        public virtual Passenger? Passenger { get; set; }

        [Required]
        public int TripId { get; set; }
        [ForeignKey("TripId")]
        public virtual Trip? Trip { get; set; } 

        [Required]
        public int StationId { get; set; }
        [ForeignKey("StationId")]
        public virtual Station? Station { get; set; }

        public virtual ICollection<PassengerDetails> Passengers { get; set; } = new List<PassengerDetails>();

        [Range(1, 10, ErrorMessage = "You can book between 1 to 10 seats.")]
        public int NumberOfSeats { get; set; } = 1;

        public decimal TotalPrice { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime BookingAt { get; set; } 
    }

   
}