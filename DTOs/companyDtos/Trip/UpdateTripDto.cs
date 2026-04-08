namespace Darb.Api.Dtos
{
    public class UpdateTripDto
    {
     

        public DateTime? DepartureDateTime { get; set; }

        public DateTime? ArrivalDateTime { get; set; }

        public int? BusId { get; set; }

    }
}