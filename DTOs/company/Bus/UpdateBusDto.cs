using Darb.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Dtos
{
    public class UpdateBusDto
    {
        public string? Model { get; set; }

        [Range(5, 100, ErrorMessage = "يجب أن تكون السعة بين 5 و 100 مقعد.")]
        public int? Capacity { get; set; }

        public BusStatus? Status { get; set; }
    }
}