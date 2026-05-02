using Darb.Api.Helpers;
using Darb.Api.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public enum BookingStatus
    {
        // 0: Pending (Waiting for receipt upload)
        PendingAttachment = 0,

        // 1: Awaiting Confirmation (Receipt uploaded)
        AwaitingConfirmation = 1,

        // 2: Confirmed (Receipt accepted)
        Confirmed = 2,

        // 3: Cancelled
        Cancelled = 3,

        Completed = 4
    }

    public class Booking
    {
        [Key]
        public int BookingId { get; set; }


        // --- Foreign Keys ---

        [Required]
        public int PassengerId { get; set; }

        [ForeignKey("PassengerId")]
        public virtual Passenger? Passenger { get; set; }

        [Required]
        public int TripScheduleId { get; set; }

        [ForeignKey("TripScheduleId")]
        public virtual TripSchedule? TripSchedule { get; set; } 

        public virtual ICollection<PassengerDetails> Passengers { get; set; } = new List<PassengerDetails>();

        public int ReservedSeatsCount { get; set; } 

        public decimal TotalAmount { get; set; }
        
        public string? ReceiptImagePath { get; set; }

        [Required]
        public BookingStatus Status { get; set; } 

        public DateTime BookingAt { get; set; } 
    }

   
}
