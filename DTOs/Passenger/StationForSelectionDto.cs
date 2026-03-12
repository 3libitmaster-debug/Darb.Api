namespace Darb.Api.DTOs.Station
{
    public class StationForSelectionDto
    {
        public int StationId { get; set; }
        public string ?CityName { get; set; }
        public string ?Address { get; set; } 
        public TimeSpan DurationToEndStation { get; set; }
        public decimal ExtraFee { get; set; }



    }
}
