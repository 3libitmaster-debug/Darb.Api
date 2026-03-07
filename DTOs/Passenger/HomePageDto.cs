namespace Darb.Api.DTOs.Passenger
{
    public class HomePageDto
    {
        public List<AdCardDto> AdCards { get; set; } = new();

        public SearchCardDto SearchCard { get; set; } = new();
    }
}