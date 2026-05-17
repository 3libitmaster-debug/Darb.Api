using Darb.Api.Helpers;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Core.Entities
{

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } 

        [Required, MaxLength(500)]
        public string Body { get; set; } 

        public bool IsRead { get; set; } = false; 

        public DateTime CreatedAt { get; set; } = DateHelper.GetYemenTime();

        // المستلم (الراكب أو موظف الشركة)
        [Required]
        public int ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual User ?Receiver { get; set; }

        [Required]
        public NotificationCategory NotificationType { get; set; }

        [Required]
        public SenderRole SenderType { get; set; }

        public int? SenderCompanyId { get; set; }

        [ForeignKey("SenderCompanyId")]
        public virtual Company? SenderCompany { get; set; }
    }
}