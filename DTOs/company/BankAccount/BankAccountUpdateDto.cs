using System.ComponentModel.DataAnnotations;

namespace Darb.Api.DTOs.BankAccount
{
    public class BankAccountUpdateDto
    {
        [MaxLength(50)]
        public string? AccountNumber { get; set; }

        [MaxLength(150)]
        public string? AccountHolderName { get; set; }

        public int? BankId { get; set; }
    }
}
