using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.customer;
using Darb.Api.DTOs.passengerDtos.bookingDtos;
using Darb.Api.DTOs.passengerDtos.homePageDtos;
using Darb.Api.DTOs.passengerDtos.settings;
using Darb.Api.Models.Enums;

namespace Darb.Api.Services.Interfaces
{
    public interface ICustomerService
    {
        
        // Retrieves all data required for the customer home page 
        Task<ResponseDto> GetHomePageDataAsync();
        Task<ResponseDto> SearchTripsAsync(TripSearchQueryDto query);
        Task<ResponseDto> GetTripStationsAsync(int tripId);
        Task<ResponseDto> GetCompanyBankAccountsAsync(int companyId);
        Task<ResponseDto> GetProfileAsync(int customerId);
        Task<ResponseDto> UpdateProfileAsync(int customerId, UpdateCustomerProfileDto request);
        Task<ResponseDto> BookTripAsync(int UserId, BookingRequestDto request);
        Task<ResponseDto> UploadReceiptAsync(int UserId, UploadReceiptDto request);


        Task<ResponseDto> GetBookingStatusesAsync();
        Task<ResponseDto> GetBookingsByStatusAsync(int customerId, BookingStatus status);
        Task<ResponseDto> GetBookingDetailsAsync(int bookingId, int customerId);
        Task<ResponseDto> RequestBookingCancellationAsync(int bookingId, int customerId);

        #region Review Management Methods

        /// <summary>
        /// Submits a new review for a company using specialized AddReviewDto.
        /// </summary>
        /// <param name="customerId">The ID of the authenticated customer.</param>
        /// <param name="request">Contains CompanyId, Rating, and Description.</param>
        Task<ResponseDto> AddReviewAsync(int customerId, AddReviewDto request);

        /// <summary>
        /// Retrieves all reviews submitted by the specific customer, returned as ReviewReturnDto.
        /// </summary>
        /// <param name="customerId">The ID of the customer fetching their history.</param>
        Task<ResponseDto> GetPassengerReviewsAsync(int customerId);

        /// <summary>
        /// Updates an existing review's content using UpdateReviewDto and recalculates the company's average.
        /// </summary>
        /// <param name="customerId">The customer ID for ownership verification.</param>
        /// <param name="reviewId">The unique identifier of the review.</param>
        /// <param name="request">Contains updated Rating and Description.</param>
        Task<ResponseDto> UpdateReviewAsync(int customerId, int reviewId, UpdateReviewDto request);

        /// <summary>
        /// Permanently deletes a review and triggers a rating recalculation for the associated company.
        /// </summary>
        /// <param name="customerId">The customer ID for ownership verification.</param>
        /// <param name="reviewId">The ID of the review to be removed.</param>
        Task<ResponseDto> DeleteReviewAsync(int customerId, int reviewId);

        /// <summary>
        /// Retrieves the details of a single review, mapped to ReviewReturnDto.
        /// </summary>
        /// <param name="reviewId">The unique ID of the review.</param>
        Task<ResponseDto> GetReviewByIdAsync(int reviewId);

        /// <summary>
        /// Retrieves all reviews for a specific company by its ID.
        /// </summary>
        /// <param name="companyId">The ID of the company.</param>
        Task<ResponseDto> GetCompanyReviewsAsync(int companyId);

        #endregion

        #region Complaint Management Methods

        /// <summary>
        /// إرسال شكوى ضد شركة نقل معينة
        /// </summary>
        Task<ResponseDto> SubmitCompanyComplaintAsync(int customerId, Darb.Api.DTOs.customer.Complaints.SubmitCompanyComplaintDto dto);

        /// <summary>
        /// إرسال شكوى دعم فني عام
        /// </summary>
        Task<ResponseDto> SubmitTechnicalComplaintAsync(int customerId, Darb.Api.DTOs.customer.Complaints.SubmitTechnicalComplaintDto dto);

        /// <summary>
        /// جلب جميع شكاوي العميل الحالي
        /// </summary>
        Task<ResponseDto> GetMyComplaintsAsync(int customerId);

        /// <summary>
        /// جلب تفاصيل شكوى محددة (تخص العميل الحالي)
        /// </summary>
        Task<ResponseDto> GetComplaintByIdAsync(int complaintId, int customerId);

        /// <summary>
        /// تعديل شكوى (مسموح فقط إذا كانت بحالة Pending)
        /// </summary>
        Task<ResponseDto> UpdateComplaintAsync(int complaintId, int customerId, Darb.Api.DTOs.customer.Complaints.UpdateComplaintDto dto);

        /// <summary>
        /// حذف شكوى (مسموح فقط إذا كانت بحالة Pending)
        /// </summary>
        Task<ResponseDto> DeleteComplaintAsync(int complaintId, int customerId);

        #endregion
    }
}