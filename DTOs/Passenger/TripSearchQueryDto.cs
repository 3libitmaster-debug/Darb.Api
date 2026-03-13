namespace Darb.Api.DTOs.Passenger
{
    public class TripSearchQueryDto
    {
        public int? FromGovernorateId { get; set; } 
        public int? ToGovernorateId { get; set; }   
        public int? CompanyId { get; set; }         
        public int? PeriodValue { get; set; }
        public DateTime? Date { get; set; }
    }
}
