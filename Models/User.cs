using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Darb.Api.Models.Enums;
using Darb.Core.Entities; 

namespace Darb.Api.Models
{
    public class User
    {
        [Key] 
        public int UserId { get; set; }

        public string? Email { get; set; }

        [Required, MaxLength(100)]
        public string? Password { get; set; }

        [Required]
        public AccountRoles Role { get; set; }

        [Required]
        public bool IsActive { get; set; } = false;

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;


        public Customer? Customer { get; set; }
        public Company? Company { get; set; }

        public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();

        public virtual ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
    }
}