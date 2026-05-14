using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.passenger;
using Darb.Api.DTOs.passengerDtos.bookingDtos;
using Darb.Api.DTOs.passengerDtos.homePageDtos;
using Darb.Api.DTOs.passengerDtos.settings;
using Darb.Api.Extensions;
using Darb.Api.Models.Enums;
using Darb.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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


        [HttpGet("trip/stations/{tripId}")]
        [SwaggerOperation(
            Summary = "Get trip stations",
            Description = "Retrieves all stations for a specific trip")]
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


        [HttpPost("trips/book")]
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

        [HttpPost("upload/booking/receipt")]
        [Authorize(Roles = "Passenger")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
            Summary = "Upload Payment Receipt for (Stage 2)",
            Description = "Upload the payment receipt image for a previously created booking. This confirms the booking.")]
        public async Task<IActionResult> UploadReceipt([FromForm] UploadReceiptDto request)
        {
            try
            {
                int accountId = User.GetPassengerId();
                var response = await _passengerService.UploadReceiptAsync(accountId, request);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }


        [HttpGet("bookings/statuses")]
        [SwaggerOperation(
        Summary = "Get Booking Statuses",
        Description = "Returns a lookup list of booking status IDs and their Arabic descriptions.")]
        public async Task<IActionResult> GetBookingStatuses()
        {
            var response = await _passengerService.GetBookingStatusesAsync();
            return Ok(response);
        }



        [HttpGet("bookings/status")] 
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(Summary = "Get Passenger Bookings by StatusId.")]
        public async Task<IActionResult> GetBookingsByStatus(BookingStatus status) 
        {
            int passengerId = User.GetPassengerId();
            var response = await _passengerService.GetBookingsByStatusAsync(passengerId, status);

            return Ok(response);
        }


        [HttpGet("bookings")]
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

        #region Passenger Reviews Endpoints

        /// <summary>
        /// Retrieves the details of a specific review using its unique ID.
        /// </summary>
        [HttpGet("reviews/{reviewId}")]
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(Summary = "Get Review By ID")]
        public async Task<IActionResult> GetReviewById(int reviewId)
        {
            var response = await _passengerService.GetReviewByIdAsync(reviewId);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        /// <summary>
        /// Submits a new review for a company.
        /// Uses AddReviewDto to ensure CompanyId is provided.
        /// </summary>
        [HttpPost("reviews")]
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(Summary = "Add Company Review")]
        public async Task<IActionResult> AddReview([FromBody] AddReviewDto request) // تم التعديل هنا
        {
            try
            {
                int passengerId = User.GetPassengerId();

                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseDto.FailureResponse("بيانات التقييم غير مكتملة أو غير صالحة."));
                }

                var response = await _passengerService.AddReviewAsync(passengerId, request);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseDto.FailureResponse($"An unexpected error occurred: {ex.Message}"));
            }
        }

        /// <summary>
        /// Returns a list of all reviews submitted by the currently authenticated passenger.
        /// </summary>
        [HttpGet("reviews")]
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(Summary = "Get My Reviews")]
        public async Task<IActionResult> GetMyReviews()
        {
            int passengerId = User.GetPassengerId();
            var response = await _passengerService.GetPassengerReviewsAsync(passengerId);
            return Ok(response);
        }

        /// <summary>
        /// Updates an existing review's rating and comment.
        /// Uses UpdateReviewDto as required by the service layer.
        /// </summary>
        [HttpPut("reviews/{reviewId}")]
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(Summary = "Update Review")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateReviewDto request) // تم التعديل هنا لحل الخطأ CS1503
        {
            int passengerId = User.GetPassengerId();

            // الآن المتغير 'request' من نوع UpdateReviewDto سيتوافق تماماً مع توقيع الميثود في الخدمة
            var response = await _passengerService.UpdateReviewAsync(passengerId, reviewId, request);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Deletes a specific review and updates the associated company's average rating.
        /// </summary>
        [HttpDelete("reviews/{reviewId}")]
        [Authorize(Roles = "Passenger")]
        [SwaggerOperation(Summary = "Delete Review")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            int passengerId = User.GetPassengerId();
            var response = await _passengerService.DeleteReviewAsync(passengerId, reviewId);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        #endregion

    }
}