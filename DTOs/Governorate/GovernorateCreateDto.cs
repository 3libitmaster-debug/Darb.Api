using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.Governorate
{
    public class GovernorateCreateDto 
    {
        [Required] 
        public string? Name { get; set; } }

}
