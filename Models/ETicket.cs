using Darb.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class ETicket
    {
        [Key]
        public int Id { get; set; }

        public int PassengerDetailId { get; set; }
        [ForeignKey("PassengerDetailId")]
        public virtual PassengerDetails? PassengerDetails { get; set; }

        public string? TicketCode { get; set; }

        public ETicketStatus Status { get; set; } = ETicketStatus.UnValid;
    }
}
