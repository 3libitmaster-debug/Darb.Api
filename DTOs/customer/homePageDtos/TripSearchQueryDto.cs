namespace Darb.Api.DTOs.passengerDtos.homePageDtos
{
    public class TripSearchQueryDto
    {
        public int? FromGovernorateId { get; set; } 
        public int? ToGovernorateId { get; set; }   
        public int? CompanyId { get; set; }         
        public int? PeriodId { get; set; }
        public DateTime? Date { get; set; }
    }
}
