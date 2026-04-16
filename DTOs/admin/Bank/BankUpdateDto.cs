using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Darb.Api.DTOs.adminDtos.Bank
{
    public class BankUpdateDto
    {
        [MaxLength(150)]
        public string? BankName { get; set; }

        public IFormFile? LogoFile { get; set; }
    }
}
