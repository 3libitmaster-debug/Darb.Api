using Darb.Api.Dtos;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.BankAccount;
using Darb.Api.DTOs.TripFare;
using Darb.Api.DTOs.TripSchedule;
using Darb.Api.Extensions;
using Darb.Api.Models;
using Darb.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Darb.Api.Controllers
{
    [Authorize(Roles = "Company")]
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        #region Trip Management Endpoints

        [HttpGet("trips")]
        [SwaggerOperation(
            Summary = "Get All Company Trips",
            Description = "Retrieves all scheduled, completed, and cancelled trips belonging to the authenticated company.")]
        public async Task<IActionResult> GetMyTrips()
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
            {
                return Unauthorized(ResponseDto.FailureResponse("عذراً، لم يتم العثور على بيانات تعريف الشركة في رمز التحقق."));
            }

            var response = await _companyService.GetAllCompanyTripsAsync(companyId);
            return Ok(response);
        }

        [HttpGet("trips/{id}")]
        [SwaggerOperation(
            Summary = "Get Trip By ID",
            Description = "Fetches detailed information for a specific trip using its unique identifier.")]
        public async Task<IActionResult> GetTrip(int id)
        {
            int companyId = User.GetCompanyId();
            var response = await _companyService.GetTripByIdAsync(id, companyId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        [HttpPost("trips")]
        [SwaggerOperation(
            Summary = "Create New Trip",
            Description = "Allows the company to schedule a new trip by providing bus details, route, and price.")]
        public async Task<IActionResult> AddTrip([FromBody] CreateTripDto tripDto)
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("نأسف، هويّة الشركة مفقودة في رمز الأمان الخاص بك."));

            var response = await _companyService.createTripAsync(tripDto, companyId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPut("trips/{id}")]
        [SwaggerOperation(
            Summary = "Update Trip Details",
            Description = "Updates existing trip information such as price, timing, or bus assignment. Only non-completed trips can be updated.")]
        public async Task<IActionResult> UpdateTrip(int id, [FromBody] UpdateTripDto updateDto)
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
            {
                return Unauthorized(ResponseDto.FailureResponse("خطأ أمني: بيانات تعريف الشركة غير صالحة أو مفقودة."));
            }

            var response = await _companyService.UpdateTripAsync(id, updateDto, companyId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("trips/{id}")]
        [SwaggerOperation(
            Summary = "Delete a Trip",
            Description = "Permanently removes a trip record from the system. Note: Completed trips cannot be deleted for audit purposes.")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            int companyId = User.GetCompanyId();
            var response = await _companyService.DeleteTripAsync(id, companyId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        #endregion

        #region Bus Management Endpoints

        [HttpGet("buses")]
        [SwaggerOperation(
            Summary = "Get All Company Buses",
            Description = "Retrieves a comprehensive list of all buses owned by the authenticated company.")]
        public async Task<IActionResult> GetFleet()
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("عذراً، لم يتم العثور على بيانات تعريف الشركة."));

            var response = await _companyService.GetAllCompanyBusesAsync(companyId);
            return Ok(response);
        }

        [HttpGet("buses/{id}")]
        [SwaggerOperation(
            Summary = "Get Bus By ID",
            Description = "Retrieves technical details and current status of a specific bus within the company's fleet.")]
        public async Task<IActionResult> GetBus(int id)
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("عذراً، بيانات تعريف الشركة غير متوفرة."));

            var response = await _companyService.GetBusByIdAsync(id, companyId);

            return response.Success ? Ok(response) : NotFound(response);
        }


        [HttpPost("buses")]
        [SwaggerOperation(
            Summary = "Create New Bus",
            Description = "Adds a new vehicle to the company's fleet by providing plate number, model, and capacity.")]
        public async Task<IActionResult> AddBus([FromBody] CreateBusDto busDto)
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("نأسف، هويّة الشركة مفقودة في رمز الأمان الخاص بك."));

            var response = await _companyService.CreateBusAsync(busDto, companyId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPut("buses/{id}")]
        [SwaggerOperation(
            Summary = "Update Bus Details",
            Description = "Modifies existing bus information or updates its operational status (Available / UnderMaintenance).")]
        public async Task<IActionResult> UpdateBus(int id, [FromBody] UpdateBusDto busDto)
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("خطأ أمني: بيانات تعريف الشركة غير صالحة."));

            var response = await _companyService.UpdateBusAsync(id, busDto, companyId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpDelete("buses/{id}")]
        [SwaggerOperation(
            Summary = "Delete Bus from Fleet",
            Description = "Permanently deletes a bus record. Note: Buses with existing trip history cannot be deleted.")]
        public async Task<IActionResult> DeleteBus(int id)
        {
            int companyId = User.GetCompanyId();

            var response = await _companyService.DeleteBusAsync(id, companyId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        #endregion

        #region Station Management Endpoints

        [HttpGet("stations")]
        [SwaggerOperation(
            Summary = "Get All Company Stations",
            Description = "Retrieves a comprehensive list of all stations owned by the authenticated company.")]
        public async Task<IActionResult> GetStations()
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("عذراً، لم يتم العثور على بيانات تعريف الشركة."));

            var response = await _companyService.GetAllCompanyStationsAsync(companyId);
            return Ok(response);
        }

        [HttpGet("stations/{id}")]
        [SwaggerOperation(
            Summary = "Get Station By ID",
            Description = "Retrieves details of a specific station within the company's network.")]
        public async Task<IActionResult> GetStation(int id)
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("عذراً، بيانات تعريف الشركة غير متوفرة."));

            var response = await _companyService.GetStationByIdAsync(id, companyId);

            return response.Success ? Ok(response) : NotFound(response);
        }

        [HttpPost("stations")]
        [SwaggerOperation(
            Summary = "Create New Station",
            Description = "Adds a new station to the company's network.")]
        public async Task<IActionResult> AddStation([FromBody] Darb.Api.DTOs.Station.CreateStationDto stationDto)
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("نأسف، هويّة الشركة مفقودة في رمز الأمان الخاص بك."));

            var response = await _companyService.CreateStationAsync(stationDto, companyId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPut("stations/{id}")]
        [SwaggerOperation(
            Summary = "Update Station Details",
            Description = "Modifies existing station information.")]
        public async Task<IActionResult> UpdateStation(int id, [FromBody] Darb.Api.DTOs.Station.UpdateStationDto stationDto)
        {
            int companyId = User.GetCompanyId();

            if (companyId == 0)
                return Unauthorized(ResponseDto.FailureResponse("خطأ أمني: بيانات تعريف الشركة غير صالحة."));

            var response = await _companyService.UpdateStationAsync(id, stationDto, companyId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpDelete("stations/{id}")]
        [SwaggerOperation(
            Summary = "Delete Station",
            Description = "Permanently deletes a station record.")]
        public async Task<IActionResult> DeleteStation(int id)
        {
            int companyId = User.GetCompanyId();

            var response = await _companyService.DeleteStationAsync(id, companyId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        #endregion

        #region Booking Management Endpoints

        [HttpGet("trip/bookings")]
        [SwaggerOperation(Summary = "Get All Bookings", Description = "Retrieves a comprehensive list of all bookings for trips owned by the authenticated company.")]
        public async Task<IActionResult> GetBookings()
        {
            int companyId = User.GetCompanyId();
            if (companyId == 0) return Unauthorized(ResponseDto.FailureResponse("عذراً، بيانات تعريف الشركة غير متوفرة."));

            var response = await _companyService.GetAllCompanyBookingsAsync(companyId);
            return Ok(response);
        }

        [HttpGet("trip/bookings/{id}")]
        [SwaggerOperation(Summary = "Get Booking By ID", Description = "Retrieves detailed information about a specific booking and its passengers.")]
        public async Task<IActionResult> GetBooking(int id)
        {
            int companyId = User.GetCompanyId();
            if (companyId == 0) return Unauthorized(ResponseDto.FailureResponse("عذراً، بيانات تعريف الشركة غير متوفرة."));

            var response = await _companyService.GetCompanyBookingByIdAsync(id, companyId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        [HttpPut("trip/bookings/{id}/status")]
        [SwaggerOperation(Summary = "Update Booking Status", Description = "Modifies the status of a booking (e.g. to Confirmed). Confirming generates ETickets.")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] Darb.Api.DTOs.Booking.CompanyUpdateBookingStatusDto dto)
        {
            int companyId = User.GetCompanyId();
            if (companyId == 0) return Unauthorized(ResponseDto.FailureResponse("عذراً، بيانات تعريف الشركة غير متوفرة."));

            var response = await _companyService.UpdateCompanyBookingStatusAsync(id, dto, companyId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("trip/bookings/{id}")]
        [SwaggerOperation(Summary = "Delete Booking", Description = "Permanently removes a booking from the system. Confirmed bookings must be cancelled first.")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            int companyId = User.GetCompanyId();
            if (companyId == 0) return Unauthorized(ResponseDto.FailureResponse("عذراً، بيانات تعريف الشركة غير متوفرة."));

            var response = await _companyService.DeleteCompanyBookingAsync(id, companyId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        #endregion

        #region BankAccount Management Endpoints

        [HttpGet("bank/accounts")]
        [SwaggerOperation(Summary = "Get All Bank Accounts", Description = "Retrieves a list of all bank accounts for the authenticated company.")]
        public async Task<IActionResult> GetBankAccounts() 
            => Ok(await _companyService.GetAllBankAccountsAsync(User.GetCompanyId()));

        [HttpGet("bank/accounts/{id}")]
        [SwaggerOperation(Summary = "Get Bank Account by ID", Description = "Retrieves detailed information about a specific bank account.")]
        public async Task<IActionResult> GetBankAccount(int id) 
            => Ok(await _companyService.GetBankAccountByIdAsync(id, User.GetCompanyId()));

        [HttpPost("bank/accounts")]
        [SwaggerOperation(Summary = "Add New Bank Account", Description = "Creates a new bank account record for the company.")]
        public async Task<IActionResult> CreateBankAccount([FromBody] BankAccountCreateDto dto) 
            => Ok(await _companyService.CreateBankAccountAsync(dto, User.GetCompanyId()));

        [HttpPut("bank/accounts/{id}")]
        [SwaggerOperation(Summary = "Update Bank Account", Description = "Modifies an existing bank account's details.")]
        public async Task<IActionResult> UpdateBankAccount(int id, [FromBody] BankAccountUpdateDto dto) 
            => Ok(await _companyService.UpdateBankAccountAsync(id, dto, User.GetCompanyId()));

        [HttpDelete("bank/accounts/{id}")]
        [SwaggerOperation(Summary = "Delete Bank Account", Description = "Permanently removes a bank account from the system.")]
        public async Task<IActionResult> DeleteBankAccount(int id) 
            => Ok(await _companyService.DeleteBankAccountAsync(id, User.GetCompanyId()));

        #endregion

        #region Trip Fare Management Endpoints

        [HttpGet("trip/fares")]
        [SwaggerOperation(Summary = "Get All Trip Fares", Description = "Retrieves a comprehensive list of all trip fares for the authenticated company.")]
        public async Task<IActionResult> GetTripFares() 
            => Ok(await _companyService.GetAllCompanyTripFaresAsync(User.GetCompanyId()));

        [HttpGet("trip/fares/{id}")]
        [SwaggerOperation(Summary = "Get Trip Fare by ID", Description = "Retrieves detailed information about a specific trip fare.")]
        public async Task<IActionResult> GetTripFare(int id) 
            => Ok(await _companyService.GetTripFareByIdAsync(id, User.GetCompanyId()));

        [HttpPost("trip/fares")]
        [SwaggerOperation(Summary = "Add New Trip Fare", Description = "Creates a new trip fare mapping for a specific destination and station.")]
        public async Task<IActionResult> CreateTripFare([FromBody] CreateTripFareDto dto) 
            => Ok(await _companyService.CreateTripFareAsync(dto, User.GetCompanyId()));

        [HttpPut("trip/fares/{id}")]
        [SwaggerOperation(Summary = "Update Trip Fare", Description = "Modifies an existing trip fare's price and time offset details.")]
        public async Task<IActionResult> UpdateTripFare(int id, [FromBody] UpdateTripFareDto dto) 
            => Ok(await _companyService.UpdateTripFareAsync(id, dto, User.GetCompanyId()));

        [HttpDelete("trip/fares/{id}")]
        [SwaggerOperation(Summary = "Delete Trip Fare", Description = "Permanently removes a trip fare mapping from the system.")]
        public async Task<IActionResult> DeleteTripFare(int id) 
            => Ok(await _companyService.DeleteTripFareAsync(id, User.GetCompanyId()));

        #endregion

        #region Trip Schedule Management Endpoints

        [HttpGet("trips/{tripId}/schedules")]
        [SwaggerOperation(Summary = "Get All Trip Schedules", Description = "Retrieves all station stops for a specific trip.")]
        public async Task<IActionResult> GetTripSchedules(int tripId)
            => Ok(await _companyService.GetAllTripSchedulesAsync(tripId, User.GetCompanyId()));

        [HttpGet("trips/schedules/{id}")]
        [SwaggerOperation(Summary = "Get Trip Schedule by ID", Description = "Retrieves details of a specific station stop.")]
        public async Task<IActionResult> GetTripSchedule(int id)
            => Ok(await _companyService.GetTripScheduleByIdAsync(id, User.GetCompanyId()));

        [HttpPost("trips/schedules")]
        [SwaggerOperation(Summary = "Add New Trip Schedule stop", Description = "Adds a new station stop to an existing trip.")]
        public async Task<IActionResult> CreateTripSchedule([FromBody] AddTripScheduleDto dto)
            => Ok(await _companyService.AddTripScheduleAsync(dto, User.GetCompanyId()));

        [HttpPut("trips/schedules/{id}")]
        [SwaggerOperation(Summary = "Update Trip Schedule", Description = "Modifies an existing station stop's time or fare.")]
        public async Task<IActionResult> UpdateTripSchedule(int id, [FromBody] UpdateTripScheduleDto dto)
            => Ok(await _companyService.UpdateTripScheduleAsync(id, dto, User.GetCompanyId()));

        [HttpDelete("trips/schedules/{id}")]
        [SwaggerOperation(Summary = "Delete Trip Schedule stop", Description = "Permanently removes a station stop from a trip.")]
        public async Task<IActionResult> DeleteTripSchedule(int id)
            => Ok(await _companyService.DeleteTripScheduleAsync(id, User.GetCompanyId()));

        #endregion
    }
}
