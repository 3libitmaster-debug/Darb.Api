using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.adminDtos.Governorate
{
    public class GovernorateCreateDto 
    {
        [Required] 
        public string? Name { get; set; } }

}
