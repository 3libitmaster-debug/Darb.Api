namespace Darb.Api.DTOs.TripFare
{
    public class TripFareReadDto
    {
        public int TripFareId { get; set; }
        public int FromGovId { get; set; }
        public string? FromGovernorateName { get; set; }
        
        public int ToGovId { get; set; }
        public string? ToGovernorateName { get; set; }

        public int StationId { get; set; }
        public string? CityName { get; set; }

        public decimal Price { get; set; }
        public int MinutesOffset { get; set; }
        public int CompanyId { get; set; }
    }
}
