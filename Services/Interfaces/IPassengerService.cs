using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.passengerDtos.bookingDtos;
using Darb.Api.DTOs.passengerDtos.homePageDtos;

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
        Task<ResponseDto> BookTripAsync(int userId, BookingRequestDto request);
        Task<ResponseDto> UploadReceiptAsync(int userId, UploadReceiptDto request);
        Task<ResponseDto> GetMyBookingsAsync(int passengerId);
    }
}