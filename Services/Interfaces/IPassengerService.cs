using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.passenger;
using Darb.Api.DTOs.passengerDtos.bookingDtos;
using Darb.Api.DTOs.passengerDtos.homePageDtos;
using Darb.Api.DTOs.passengerDtos.settings;

namespace Darb.Api.Services.Interfaces
{
    public interface IPassengerService
    {
        
        // Retrieves all data required for the passenger home page 
        Task<ResponseDto> GetHomePageDataAsync();
        Task<ResponseDto> SearchTripsAsync(TripSearchQueryDto query);
        Task<ResponseDto> GetTripStationsAsync(int tripId);
        Task<ResponseDto> GetCompanyBankAccountsAsync(int companyId);
        Task<ResponseDto> GetProfileAsync(int passengerId);
        Task<ResponseDto> BookTripAsync(int AccountId, BookingRequestDto request);
        Task<ResponseDto> UploadReceiptAsync(int AccountId, UploadReceiptDto request);

        Task<ResponseDto> GetBookingStatusesAsync();
        Task<ResponseDto> GetMyBookingsAsync(int passengerId);

        #region Review Management Methods

        /// <summary>
        /// Submits a new review for a company using specialized AddReviewDto.
        /// </summary>
        /// <param name="passengerId">The ID of the authenticated passenger.</param>
        /// <param name="request">Contains CompanyId, Rating, and Description.</param>
        Task<ResponseDto> AddReviewAsync(int passengerId, AddReviewDto request);

        /// <summary>
        /// Retrieves all reviews submitted by the specific passenger, returned as ReviewReturnDto.
        /// </summary>
        /// <param name="passengerId">The ID of the passenger fetching their history.</param>
        Task<ResponseDto> GetPassengerReviewsAsync(int passengerId);

        /// <summary>
        /// Updates an existing review's content using UpdateReviewDto and recalculates the company's average.
        /// </summary>
        /// <param name="passengerId">The passenger ID for ownership verification.</param>
        /// <param name="reviewId">The unique identifier of the review.</param>
        /// <param name="request">Contains updated Rating and Description.</param>
        Task<ResponseDto> UpdateReviewAsync(int passengerId, int reviewId, UpdateReviewDto request);

        /// <summary>
        /// Permanently deletes a review and triggers a rating recalculation for the associated company.
        /// </summary>
        /// <param name="passengerId">The passenger ID for ownership verification.</param>
        /// <param name="reviewId">The ID of the review to be removed.</param>
        Task<ResponseDto> DeleteReviewAsync(int passengerId, int reviewId);

        /// <summary>
        /// Retrieves the details of a single review, mapped to ReviewReturnDto.
        /// </summary>
        /// <param name="reviewId">The unique ID of the review.</param>
        Task<ResponseDto> GetReviewByIdAsync(int reviewId);

        #endregion
    }
}