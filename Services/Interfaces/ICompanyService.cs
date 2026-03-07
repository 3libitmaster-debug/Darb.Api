using Darb.Api.Dtos;
using Darb.Api.DTOs.Base;

namespace Darb.Api.Services.Interfaces
{
    public interface ICompanyService
    {
        #region Trip Management
        // Retrieves all trips belonging to the specific company with their full details.
        Task<ResponseDto> GetAllCompanyTripsAsync(int companyId);

        // Creates a new trip after validating bus ownership and checking for scheduling conflicts.
        Task<ResponseDto> createTripAsync(CreateTripDto tripDto, int companyId);

        // Updates an existing trip's details partially, allowed only if the trip is still 'Scheduled'.
        Task<ResponseDto> UpdateTripAsync(int tripId, UpdateTripDto updateDto, int companyId);

        // Fetches a single trip's details by its ID, ensuring it belongs to the company.
        Task<ResponseDto> GetTripByIdAsync(int tripId, int companyId);

        // Permanently deletes a trip from the database after verifying ownership and status.
        Task<ResponseDto> DeleteTripAsync(int tripId, int companyId);
        #endregion


        #region Bus Management
        // Retrieves all buses in the company's fleet, filtered by the company ID.
        Task<ResponseDto> GetAllCompanyBusesAsync(int companyId);

        // Fetches detailed information for a specific bus belonging to the company.
        Task<ResponseDto> GetBusByIdAsync(int busId, int companyId);

        // Registers a new bus into the company's fleet with technical specifications.
        Task<ResponseDto> CreateBusAsync(CreateBusDto busDto, int companyId);

        // Updates bus details or changes its operational status (Available/Maintenance).
        Task<ResponseDto> UpdateBusAsync(int busId, UpdateBusDto busDto, int companyId);

        // Removes a bus from the fleet, ensuring it has no active trip history.
        Task<ResponseDto> DeleteBusAsync(int busId, int companyId);
        #endregion

        #region Station Management
        // Retrieves all stations owned by the company.
        Task<ResponseDto> GetAllCompanyStationsAsync(int companyId);

        // Fetches detailed information for a specific station belonging to the company.
        Task<ResponseDto> GetStationByIdAsync(int stationId, int companyId);

        // Registers a new station for the company.
        Task<ResponseDto> CreateStationAsync(Darb.Api.DTOs.Station.CreateStationDto stationDto, int companyId);

        // Updates station details.
        Task<ResponseDto> UpdateStationAsync(int stationId, Darb.Api.DTOs.Station.UpdateStationDto stationDto, int companyId);

        // Removes a station from the company.
        Task<ResponseDto> DeleteStationAsync(int stationId, int companyId);
        #endregion
    }
}