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

        [HttpGet("home")]
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

        [HttpGet("stations/{companyId}/{governorateId}")]
        [SwaggerOperation(
            Summary = "Get Stations by Company and Governorate",
            Description = "Retrieves all stations for a specific company within a specific governorate.")]
        public async Task<IActionResult> GetStations(int companyId, int governorateId)
            => Ok(await _passengerService.GetStationsByCompanyAndGovernorateAsync(companyId, governorateId));

        [HttpGet("bank-accounts/{companyId}")]
        [SwaggerOperation(
            Summary = "Get Company Bank Accounts",
            Description = "Retrieves all bank accounts for a specific company.")]
        public async Task<IActionResult> GetCompanyBankAccounts(int companyId)
            => Ok(await _passengerService.GetCompanyBankAccountsAsync(companyId));
    }
}