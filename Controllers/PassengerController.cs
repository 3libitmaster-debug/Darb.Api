using Darb.Api.DTOs.Base;
using Darb.Api.Services.Interfaces;
using Darb.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Darb.Api.DTOs.passengerDtos.homePageDtos;
using Darb.Api.DTOs.passengerDtos.bookingDtos;

namespace Darb.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        [HttpGet("home/search/card")]
        [SwaggerOperation(
            Summary = "Get Search Card Data Only",
            Description = "Retrieves only the search card data (Governorates, Companies, and Periods) for the mobile app.")]
        public async Task<IActionResult> GetSearchCard()
        {
            var homePageResult = await _passengerService.GetHomePageDataAsync();
            if (homePageResult.Data is Darb.Api.DTOs.passengerDtos.homePageDtos.HomePageDto homePageDto)
                return Ok(homePageDto.SearchCard);
            return Ok(null);
        }

        [HttpGet("home/ads")]
        [SwaggerOperation(
            Summary = "Get Ads Cards Data Only",
            Description = "Retrieves only the ads cards data for the mobile app.")]
        public async Task<IActionResult> GetAdsCards()
        {
            var homePageResult = await _passengerService.GetHomePageDataAsync();
            if (homePageResult.Data is Darb.Api.DTOs.passengerDtos.homePageDtos.HomePageDto homePageDto)
                return Ok(homePageDto.AdCards);
            return Ok(null);
        }

        [HttpGet("home/companies/avatar")]
        [SwaggerOperation(
            Summary = "Get Companies Avatars",
            Description = "Returns a list of companies with their name and logo for avatar display.")]
        public async Task<IActionResult> GetCompaniesAvatar()
        {
            var homePageResult = await _passengerService.GetHomePageDataAsync();
            if (homePageResult.Data is Darb.Api.DTOs.passengerDtos.homePageDtos.HomePageDto homePageDto)
                return Ok(homePageDto.SearchCard.Companies);
            return Ok(null);
        }

        [HttpPost("home/search")]
        [SwaggerOperation(
            Summary = "Search for Trips ",
            Description = "Filters scheduled trips based on optional criteria: From/To Governorates, Company, Period, and Travel Date. If no filters are provided, it returns all scheduled trips.")]
        public async Task<IActionResult> SearchTrips([FromBody] TripSearchQueryDto query)
            => Ok(await _passengerService.SearchTripsAsync(query));


        [HttpGet("stations/dropdownMenu/{tripId}")]
        [SwaggerOperation(
            Summary = "Get Stations by Company and Governorate",
            Description = "Retrieves all stations for a specific company within a specific governorate.")]
        public async Task<IActionResult> GetStations(int tripId)
            => Ok(await _passengerService.GetTripStationsAsync(tripId));

        [HttpGet("bank/accounts/{companyId}")]
        [SwaggerOperation(
            Summary = "Get Company Bank Accounts",
            Description = "Retrieves all bank accounts for a specific company.")]
        public async Task<IActionResult> GetCompanyBankAccounts(int companyId)
            => Ok(await _passengerService.GetCompanyBankAccountsAsync(companyId));

        [HttpGet("settings/profile")]
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(
            Summary = "Get Passenger Profile",
            Description = "Retrieves personal profile details for the authenticated passenger.")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                int passengerId = User.GetPassengerId();
                var response = await _passengerService.GetProfileAsync(passengerId);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }


        [HttpPost("trip/bookings")]
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(
            Summary = "Book a Trip (Stage 1)",
            Description = "Allows an authorized passenger to create a booking without the receipt image. Returns the BookingId to be used in Stage 2.")]
        public async Task<IActionResult> BookTrip([FromBody] BookingRequestDto request)
        {
            try
            {
                int userId = User.GetPassengerId();
                var response = await _passengerService.BookTripAsync(userId, request);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }

        [HttpPost("upload/receipt")]
        [Authorize(Roles = "Passenger")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
            Summary = "Upload Payment Receipt (Stage 2)",
            Description = "Upload the payment receipt image for a previously created booking. This confirms the booking.")]
        public async Task<IActionResult> UploadReceipt([FromForm] UploadReceiptDto request)
        {
            try
            {
                int userId = User.GetPassengerId();
                var response = await _passengerService.UploadReceiptAsync(userId, request);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }

        [HttpGet("my-bookings")]
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(
            Summary = "Get Passenger Bookings",
            Description = "Retrieves all bookings made by the authenticated passenger along with trip, company, and ticket details.")]
        public async Task<IActionResult> GetMyBookings()
        {
            try
            {
                int passengerId = User.GetPassengerId();
                var response = await _passengerService.GetMyBookingsAsync(passengerId);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }

    }
}