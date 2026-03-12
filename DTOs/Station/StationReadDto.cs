namespace Darb.Api.DTOs.Station
{
    public class StationReadDto
    {
        public int StationId { get; set; }
        public string Address { get; set; } = string.Empty;
        public int Order { get; set; }
        public TimeSpan DurationToEndStation { get; set; }
        public decimal ExtraFee { get; set; }
        
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;

        public int GovernorateId { get; set; }
        public string GovernorateName { get; set; } = string.Empty;

        public int CompanyId { get; set; }
    }
}
