namespace Darb.Api.Models
{
    public class Governorate
    {
        public int GovernorateId { get; set; }

        public string ?Name { get; set; }

        public ICollection<Station> ?Station { get; set; }

        public ICollection<City> ?City { get; set; }
    }
}
