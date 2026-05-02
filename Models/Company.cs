using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;


namespace Darb.Api.Models
{
    public class Company
    {
        public int CompanyId { get; set; }

        [Required, MaxLength(150)]
        public string ?Name { get; set; }

        [Required,MaxLength(255)]
        public string ?Address { get; set; }

        [Required]
        public string ?Logo { get; set; }

        [Required]
        public string ?License { get; set; }

        public int AccountId { get; set; }
        [Required]
        [ForeignKey("AccountId")]
        public Account ?Account { get; set; }

        public ICollection<CompanySubscription>? CompanySubscription { get; set; }

        public ICollection<Trip>? Trips { get; set; }

        public ICollection<Bus> ?Bus { get; set; }

        public ICollection<Station>? Station { get; set; }
        
        public ICollection<BankAccount>? BankAccounts { get; set; }

    }
}
