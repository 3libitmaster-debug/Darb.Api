using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class BankAccount
    {
        [Key]
        public int BankAccountId { get; set; }

        [Required, MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string HolderName { get; set; } = string.Empty;

        // Foreign Key for Bank
        public int BankId { get; set; }
        [ForeignKey("BankId")]
        public Bank? Bank { get; set; }

        // Foreign Key for Company
        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }
    }
}
