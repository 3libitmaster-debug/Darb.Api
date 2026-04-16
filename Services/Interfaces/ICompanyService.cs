using Darb.Api.Dtos;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.BankAccount;
using Darb.Api.DTOs.TripFare;
using Darb.Api.DTOs.TripSchedule;

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

        #region Booking Management
        Task<ResponseDto> GetAllCompanyBookingsAsync(int companyId);
        Task<ResponseDto> GetCompanyBookingByIdAsync(int bookingId, int companyId);
        Task<ResponseDto> UpdateCompanyBookingStatusAsync(int bookingId, Darb.Api.DTOs.Booking.CompanyUpdateBookingStatusDto dto, int companyId);
        Task<ResponseDto> DeleteCompanyBookingAsync(int bookingId, int companyId);
        #endregion

        #region BankAccount Management
        Task<ResponseDto> GetAllBankAccountsAsync(int companyId);
        Task<ResponseDto> GetBankAccountByIdAsync(int bankAccountId, int companyId);
        Task<ResponseDto> CreateBankAccountAsync(BankAccountCreateDto dto, int companyId);
        Task<ResponseDto> UpdateBankAccountAsync(int bankAccountId, BankAccountUpdateDto dto, int companyId);
        Task<ResponseDto> DeleteBankAccountAsync(int bankAccountId, int companyId);
        #endregion

        #region Trip Fare Management
        Task<ResponseDto> GetAllCompanyTripFaresAsync(int companyId);
        Task<ResponseDto> GetTripFareByIdAsync(int tripFareId, int companyId);
        Task<ResponseDto> CreateTripFareAsync(CreateTripFareDto dto, int companyId);
        Task<ResponseDto> UpdateTripFareAsync(int tripFareId, UpdateTripFareDto dto, int companyId);
        Task<ResponseDto> DeleteTripFareAsync(int tripFareId, int companyId);
        #endregion

        #region Trip Schedule Management
        Task<ResponseDto> GetAllTripSchedulesAsync(int tripId, int companyId);
        Task<ResponseDto> GetTripScheduleByIdAsync(int scheduleId, int companyId);
        Task<ResponseDto> AddTripScheduleAsync(AddTripScheduleDto dto, int companyId);
        Task<ResponseDto> UpdateTripScheduleAsync(int scheduleId, UpdateTripScheduleDto dto, int companyId);
        Task<ResponseDto> DeleteTripScheduleAsync(int scheduleId, int companyId);
        #endregion
    }
}