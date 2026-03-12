using Darb.Api.Dtos;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.BankAccount;
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

        [HttpGet("Get-trips")]
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

        [HttpGet("trip/{id}")]
        [SwaggerOperation(
            Summary = "Get Trip By ID",
            Description = "Fetches detailed information for a specific trip using its unique identifier.")]
        public async Task<IActionResult> GetTrip(int id)
        {
            int companyId = User.GetCompanyId();
            var response = await _companyService.GetTripByIdAsync(id, companyId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        [HttpPost("create-trip")]
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

        [HttpPut("update-trip/{id}")]
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

        [HttpDelete("delete-trip/{id}")]
        [SwaggerOperation(
            Summary = "Delete a Trip",
            Description = "Permanently removes a trip record from the system. Note: Completed trips cannot be deleted for audit purposes.")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            int companyId = User.GetCompanyId();
            var response = await _companyService.DeleteTripAsync(id, companyId);
            return response.Success ? Ok(response) : BadRequest(response);
        }


       

        [HttpGet("Get-buses")]
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

        [HttpGet("{id}")]
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


        [HttpPost("create-bus")]
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

        [HttpPut("update-bus/{id}")]
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

        [HttpDelete("delete-bus/{id}")]
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

        #region Station Management Endpoints

        [HttpGet("Get-stations")]
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

        [HttpGet("station/{id}")]
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

        [HttpPost("create-station")]
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

        [HttpPut("update-station/{id}")]
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

        [HttpDelete("delete-station/{id}")]
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


        #region Governorate Management Endpoints

        [HttpGet("Get-all-governorates")]
        [SwaggerOperation(
            Summary = "Get All Governorates",
            Description = "Retrieves a comprehensive list of all governorates in the system.")]
        public async Task<IActionResult> GetGovernorates()
        {
            var response = await _companyService.GetAllGovernoratesAsync();
            return Ok(response);
        }

        #endregion

        #region BankAccount Management Endpoints

        [HttpGet("bank-accounts")]
        [SwaggerOperation(Summary = "Get All Bank Accounts", Description = "Retrieves a list of all bank accounts for the authenticated company.")]
        public async Task<IActionResult> GetBankAccounts() 
            => Ok(await _companyService.GetAllBankAccountsAsync(User.GetCompanyId()));

        [HttpGet("bank-accounts/{id}")]
        [SwaggerOperation(Summary = "Get Bank Account by ID", Description = "Retrieves detailed information about a specific bank account.")]
        public async Task<IActionResult> GetBankAccount(int id) 
            => Ok(await _companyService.GetBankAccountByIdAsync(id, User.GetCompanyId()));

        [HttpPost("create-bank-account")]
        [SwaggerOperation(Summary = "Add New Bank Account", Description = "Creates a new bank account record for the company.")]
        public async Task<IActionResult> CreateBankAccount([FromBody] BankAccountCreateDto dto) 
            => Ok(await _companyService.CreateBankAccountAsync(dto, User.GetCompanyId()));

        [HttpPut("bank-accounts/{id}")]
        [SwaggerOperation(Summary = "Update Bank Account", Description = "Modifies an existing bank account's details.")]
        public async Task<IActionResult> UpdateBankAccount(int id, [FromBody] BankAccountUpdateDto dto) 
            => Ok(await _companyService.UpdateBankAccountAsync(id, dto, User.GetCompanyId()));

        [HttpDelete("bank-accounts/{id}")]
        [SwaggerOperation(Summary = "Delete Bank Account", Description = "Permanently removes a bank account from the system.")]
        public async Task<IActionResult> DeleteBankAccount(int id) 
            => Ok(await _companyService.DeleteBankAccountAsync(id, User.GetCompanyId()));

        #endregion
    }
}
