using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Passenger;
using Darb.Api.Services.Interfaces;
using Darb.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
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

        #region Home & Search Endpoints

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

        #endregion

        #region Information Endpoints

        [HttpGet("stations/{tripId}")]
        [SwaggerOperation(
            Summary = "Get Stations by Company and Governorate",
            Description = "Retrieves all stations for a specific company within a specific governorate.")]
        public async Task<IActionResult> GetStations(int tripId)
            => Ok(await _passengerService.GetTripStationsAsync(tripId));

        [HttpGet("bank-accounts/{companyId}")]
        [SwaggerOperation(
            Summary = "Get Company Bank Accounts",
            Description = "Retrieves all bank accounts for a specific company.")]
        public async Task<IActionResult> GetCompanyBankAccounts(int companyId)
            => Ok(await _passengerService.GetCompanyBankAccountsAsync(companyId));

        [HttpGet("profile")]
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

        #endregion

        #region Booking Endpoints

        [HttpPost("book")]
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

        [HttpPost("upload-receipt")]
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

        #endregion
    }
}