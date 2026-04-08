using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Darb.Api.DTOs.adminDtos.Bank
{
    public class BankCreateDto
    {
        [Required, MaxLength(150)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public IFormFile LogoFile { get; set; } = null!;
    }
}
