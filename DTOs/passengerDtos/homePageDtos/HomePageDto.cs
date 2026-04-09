namespace Darb.Api.DTOs.passengerDtos.homePageDtos
{
    public class HomePageDto
    {
        public List<AdCardDto> AdCards { get; set; } = new();

        public SearchCardDto SearchCard { get; set; } = new();
    }
}