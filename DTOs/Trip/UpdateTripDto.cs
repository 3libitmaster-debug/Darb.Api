namespace Darb.Api.Dtos
{
    public class UpdateTripDto
    {
     
        public decimal? BasePrice { get; set; }

        public DateTime? DepartureDateTime { get; set; }

        public DateTime? ArrivalDateTime { get; set; }

        public int? BusId { get; set; }

    }
}