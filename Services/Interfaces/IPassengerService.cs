using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Passenger.Darb.Api.DTOs.Passenger;

namespace Darb.Api.Services.Interfaces
{
    public interface IPassengerService
    {
        
        // Retrieves all data required for the passenger home page 
        Task<ResponseDto> GetHomePageDataAsync();
        Task<ResponseDto> SearchTripsAsync(TripSearchQueryDto query);
        Task<ResponseDto> GetStationsByCompanyAndGovernorateAsync(int companyId, int governorateId);
    }
}