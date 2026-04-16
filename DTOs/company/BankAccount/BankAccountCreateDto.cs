using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.BankAccount
{
    public class BankAccountCreateDto
    {
        [Required, MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string AccountHolderName { get; set; } = string.Empty;

        [Required]
        public int BankId { get; set; }
    }
}
