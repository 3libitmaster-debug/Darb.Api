using Darb.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Dtos
{
    public class UpdateBusDto
    {
        public string? Model { get; set; }

        [Range(5, 100, ErrorMessage = "íÌÈ Ãä Êßæä ÇáÓÚÉ Èíä 5 æ 100 ãŞÚÏ.")]
        public int? Capacity { get; set; }

        public BusStatus? Status { get; set; }
    }
}