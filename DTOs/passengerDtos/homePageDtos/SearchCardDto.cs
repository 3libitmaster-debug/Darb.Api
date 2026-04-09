namespace Darb.Api.DTOs.passengerDtos.homePageDtos
{
    public class SearchCardDto
    {
 
        public List<SimpleGovernorateDto> Governorates { get; set; } = new();

        public List<SimpleCompanyDto> Companies { get; set; } = new();

        public List<PeriodDto> PeriodOptions { get; set; } = new();
    }
}
