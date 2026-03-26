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
        // 0: ÇáÍÌÒ ãÈÏÆí (ÇáÑÇßÈ áã íÑİÚ ÕæÑÉ ÇáÓäÏ ÈÚÏ)
        PendingAttachment = 0,

        // 1: Êã ÑİÚ ÇáÓäÏ (ÈÇäÊÙÇÑ ãÑÇÌÚÉ ÇáÔÑßÉ æÊÃßíÏ ÇáÏİÚ)
        AwaitingConfirmation = 1,

        // 2: Êã ÇáÊÃßíÏ (ÇáÓäÏ Óáíã æÇáãŞÚÏ ÍÌÒ äåÇÆíÇğ)
        Confirmed = 2,

        // 3: ãáÛí (ÓæÇÁ ãä ÇáÑÇßÈ Ãæ áÚÏã ÕÍÉ ÇáÓäÏ)
        Cancelled = 3
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
        public int TripRouteId { get; set; }

        [ForeignKey("TripRouteId")]
        public virtual TripRoute? TripRoute { get; set; } 

        public virtual ICollection<PassengerDetails> Passengers { get; set; } = new List<PassengerDetails>();

        [Range(1, 10, ErrorMessage = "You can book between 1 to 10 seats.")]
        public int NumberOfSeats { get; set; } 

        public decimal TotalAmount { get; set; }
        
        public string? ReceiptImagePath { get; set; }

        [Required]
        public BookingStatus Status { get; set; } 

        public DateTime BookingAt { get; set; } 
    }

   
}