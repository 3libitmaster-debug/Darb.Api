namespace Darb.Api.Dtos;

public class TripDto
{
    public int TripId { get; set; }
    public int StartGoveId { get; set; }
    public int EndGoveId { get; set; }
    public decimal BasePrice { get; set; }
    public DateTime DepartureDateTime { get; set; }
    public DateTime? ArrivalDateTime { get; set; }
    public string ?Period { get; set; }
    public int BusId { get; set; }
    public int AvailableSeats { get; set; }
    public string ?Status { get; set; }
}