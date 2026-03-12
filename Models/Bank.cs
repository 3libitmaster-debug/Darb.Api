using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Models
{
    public class Bank
    {
        [Key]
        public int BankId { get; set; }

        [Required, MaxLength(150)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public string LogoUrl { get; set; } = string.Empty;

        // Navigation Properties
        public ICollection<BankAccount>? BankAccounts { get; set; }
    }
}
