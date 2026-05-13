using Darb.Api.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace Darb.Api.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        public int PassengerId { get; set; }
        [ForeignKey("PassengerId")]
        public Passenger ?Passenger { get; set; }

        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public Company ?Company { get; set; }

        public int Rating { get; set; }

        public string ?Description { get; set; }

        public DateTime ReviewDate { get; set; } = DateHelper.GetYemenTime();




    }
}
