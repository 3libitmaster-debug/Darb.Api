using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Models
{
    public enum BusStatus
    {
        Available = 0,
        UnderMaintenance = 1
    }
    public class Bus
    {
        public int BusId { get; set; }

        [Required]
        public string ?PlateNumber { get; set; }

        [Required]
        public BusStatus BusStatus { get; set; }

        [Required]
        public string? Model { get; set; }

        [Required]
        public int BusCapacity { get; set; }

        public  int CompanyId { get; set; }

        [Required]
        public virtual Company ?Company { get; set; }

        public virtual ICollection<Trip>? Trip { get; set; }

    }
}
