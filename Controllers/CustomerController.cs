using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.customer;
using Darb.Api.DTOs.passengerDtos.bookingDtos;
using Darb.Api.DTOs.passengerDtos.homePageDtos;
using Darb.Api.DTOs.passengerDtos.settings;
using Darb.Api.Extensions;
using Darb.Api.Models.Enums;
using Darb.Api.Services.Implementations;
using Darb.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Darb.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _passengerService;
        public CustomerController(ICustomerService passengerService)
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

        [HttpGet("bank/users/{companyId}")]
        [SwaggerOperation(
            Summary = "Get Company Bank Users",
            Description = "Retrieves all bank users for a specific company.")]
        public async Task<IActionResult> GetCompanyBankAccounts(int companyId)
            => Ok(await _passengerService.GetCompanyBankAccountsAsync(companyId));

        [HttpGet("settings/profile")]
        [Authorize(Roles = "Customer")]
        [SwaggerOperation(
            Summary = "Get Customer Profile",
            Description = "Retrieves personal profile details for the authenticated customer.")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                int customerId = User.GetPassengerId();
                var response = await _passengerService.GetProfileAsync(customerId);

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
        [Authorize(Roles = "Customer")]
        [SwaggerOperation(
            Summary = "Book a Trip (Stage 1)",
            Description = "Allows an authorized customer to create a booking without the receipt image. Returns the BookingId to be used in Stage 2.")]
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
        [Authorize(Roles = "Customer")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
            Summary = "Upload Payment Receipt for (Stage 2)",
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


        [HttpGet("bookings/status-banner")]
        [SwaggerOperation(
        Summary = "Get Booking Statuses",
        Description = "Returns a lookup list of booking status IDs and their Arabic descriptions.")]
        public async Task<IActionResult> GetBookingStatuses()
        {
            var response = await _passengerService.GetBookingStatusesAsync();
            return Ok(response);
        }



        [HttpGet("bookings/status")] 
        [Authorize(Roles = "Customer")]
        [SwaggerOperation(Summary = "Get Customer Bookings by StatusId.")]
        public async Task<IActionResult> GetBookingsByStatus(BookingStatus status) 
        {
            int customerId = User.GetPassengerId();
            var response = await _passengerService.GetBookingsByStatusAsync(customerId, status);

            return Ok(response);
        }


        /// <summary>
        /// Retrieves full details for a specific booking.
        /// Example: GET api/customer/bookings/5/details
        /// </summary>
        /// <param name="bookingId">The unique ID of the booking.</param>
        /// <returns>Full booking details including customers and ticket info.</returns>
        [HttpGet("bookings/{bookingId}/details")]
        [Authorize(Roles = "Customer")]
        [SwaggerOperation(
            Summary = "Get Full Booking Details",
            Description = "Returns all details related to a booking, trip, and associated customers.")]
        public async Task<IActionResult> GetBookingDetails(int bookingId)
        {
            // استخراج معرف المسافر من الـ Claims الموجودة في الـ Token
            int customerId = User.GetPassengerId();

            // استدعاء الخدمة لجلب البيانات
            var response = await _passengerService.GetBookingDetailsAsync(bookingId, customerId);

            if (!response.Success)
            {
                // إذا لم يجد الحجز أو كان لا يخص المسافر، نعيد 404
                return NotFound(response);
            }

            return Ok(response);
        }

        [Authorize(Roles = "Customer")]
        [HttpPut("bookings/{bookingId}/request-cancellation")]
        [SwaggerOperation(
            Summary = "Request Booking Cancellation",
            Description = "Changes booking status to AwaitingCancellation if it was previously Confirmed.")]
        public async Task<IActionResult> RequestBookingCancellation(int bookingId)
        {
            // استخراج معرف العميل الحالي من الـ Claims الخاصة بالـ JWT Token بشكل آمن
            // استبدل .GetUserId() بالامتداد (Extension Method) المعتمد في مشروعك
            int customerId = User.GetPassengerId();

            // استدعاء الخدمة لمعالجة الطلب
            var result = await _passengerService.RequestBookingCancellationAsync(bookingId, customerId);

            if (!result.Success)
                return BadRequest(result); // إرجاع 400 في حال عدم مطابقة الشروط أو خطأ بالبيانات

            return Ok(result); // إرجاع 200 بنجاح العملية متضمناً الـ ResponseDto الموحد
        }
    


        
        #region Customer Reviews Endpoints

        /// <summary>
        /// Retrieves the details of a specific review using its unique ID.
        /// </summary>
        [HttpGet("reviews/{reviewId}")]
        [Authorize(Roles = "Customer")]
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
        [Authorize(Roles = "Customer")]
        [SwaggerOperation(Summary = "Add Company Review")]
        public async Task<IActionResult> AddReview([FromBody] AddReviewDto request) // تم التعديل هنا
        {
            try
            {
                int customerId = User.GetPassengerId();

                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseDto.FailureResponse("بيانات التقييم غير مكتملة أو غير صالحة."));
                }

                var response = await _passengerService.AddReviewAsync(customerId, request);

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
        /// Returns a list of all reviews submitted by the currently authenticated customer.
        /// </summary>
        [HttpGet("reviews")]
        [Authorize(Roles = "Customer")]
        [SwaggerOperation(Summary = "Get My Reviews")]
        public async Task<IActionResult> GetMyReviews()
        {
            int customerId = User.GetPassengerId();
            var response = await _passengerService.GetPassengerReviewsAsync(customerId);
            return Ok(response);
        }

        /// <summary>
        /// Updates an existing review's rating and comment.
        /// Uses UpdateReviewDto as required by the service layer.
        /// </summary>
        [HttpPut("reviews/{reviewId}")]
        [Authorize(Roles = "Customer")]
        [SwaggerOperation(Summary = "Update Review")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateReviewDto request) // تم التعديل هنا لحل الخطأ CS1503
        {
            int customerId = User.GetPassengerId();

            // الآن المتغير 'request' من نوع UpdateReviewDto سيتوافق تماماً مع توقيع الميثود في الخدمة
            var response = await _passengerService.UpdateReviewAsync(customerId, reviewId, request);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Deletes a specific review and updates the associated company's average rating.
        /// </summary>
        [HttpDelete("reviews/{reviewId}")]
        [Authorize(Roles = "Customer")]
        [SwaggerOperation(Summary = "Delete Review")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            int customerId = User.GetPassengerId();
            var response = await _passengerService.DeleteReviewAsync(customerId, reviewId);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        #endregion

    }
}