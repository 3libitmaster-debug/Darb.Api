using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Passenger;
using Darb.Api.DTOs.Passenger.Darb.Api.DTOs.Passenger;
using Darb.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Darb.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [SwaggerTag("Passenger Operations: Home page and Search configurations")]
    public class PassengerController : ControllerBase
    {
        private readonly IPassengerService _passengerService;
        public PassengerController(IPassengerService passengerService)
        {
            _passengerService = passengerService;
        }

        [HttpGet("home-page")]
        [SwaggerOperation(
          Summary = "Get Home Page Data",
          Description = "Retrieves ads and search card data (Governorates, Companies, and Periods) for the mobile app home screen.")]
        public async Task<IActionResult> GetHomePage()
          => Ok(await _passengerService.GetHomePageDataAsync());

        [HttpPost("search-trips")]
        [SwaggerOperation(
            Summary = "Search for Trips ",
            Description = "Filters scheduled trips based on optional criteria: From/To Governorates, Company, Period, and Travel Date. If no filters are provided, it returns all scheduled trips.")]
        public async Task<IActionResult> SearchTrips([FromBody] TripSearchQueryDto query)
            => Ok(await _passengerService.SearchTripsAsync(query));
    }
}