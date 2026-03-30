using Darb.Api.DTOs.Advertisement;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.City;
using Darb.Api.DTOs.Governorate;
using Darb.Api.DTOs.Bank;
using Darb.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Darb.Api.Services.Interfaces;
using System.Text.Json.Serialization;

namespace Darb.Api.Controllers
{
    //[Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    [SwaggerTag("")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService) { _adminService = adminService; }

        #region Governorate Endpoints

        [HttpGet("governorates")]
        [SwaggerOperation(Summary = "Get all governorates", Description = "Retrieves a list of all governorates registered in the system.")]
        public async Task<IActionResult> GetGovs() => Ok(await _adminService.GetAllGovernoratesAsync());

        [HttpGet("governorates/{id}")]
        [SwaggerOperation(Summary = "Get governorates by ID", Description = "Retrieves detailed information about a specific governorate using its unique ID.")]
        public async Task<IActionResult> GetGov(int id) => Ok(await _adminService.GetGovernorateByIdAsync(id));

        [HttpPost("governorates")]
        [SwaggerOperation(Summary = "Add New governorates", Description = "Creates a new governorate record in the system.")]
        public async Task<IActionResult> CreateGov([FromBody] GovernorateCreateDto dto) => Ok(await _adminService.CreateGovernorateAsync(dto));

        [HttpPut("governorates/{id}")]
        [SwaggerOperation(Summary = "Update governorates", Description = "Modifies an existing governorate's details.")]
        public async Task<IActionResult> UpdateGov(int id, [FromBody] GovernorateCreateDto dto) => Ok(await _adminService.UpdateGovernorateAsync(id, dto));

        [HttpDelete("governorates/{id}")]
        [SwaggerOperation(Summary = "Delete governorates", Description = "Permanently removes a governorate from the system. Note: Only governorates without linked cities can be deleted.")]
        public async Task<IActionResult> DeleteGov(int id) => Ok(await _adminService.DeleteGovernorateAsync(id));

        #endregion

        #region City Endpoints

        [HttpGet("cities")]
        [SwaggerOperation(Summary = "Get All Cities", Description = "Retrieves a list of all cities along with their parent governorate information.")]
        public async Task<IActionResult> GetCities() => Ok(await _adminService.GetAllCitiesAsync());

        [HttpGet("cities/{id}")]
        [SwaggerOperation(Summary = "Get City by ID", Description = "Retrieves detailed information about a specific city.")]
        public async Task<IActionResult> GetCity(int id) => Ok(await _adminService.GetCityByIdAsync(id));

        [HttpGet("governorate-cities/{governorateId}")]
        [SwaggerOperation(Summary = "Get Cities by Governorate ID", Description = "Retrieves a list of cities belonging to a specific governorate.")]
        public async Task<IActionResult> GetCitiesByGov(int governorateId) => Ok(await _adminService.GetCitiesByGovernorateIdAsync(governorateId));

        [HttpPost("cities")]
        [SwaggerOperation(Summary = "Add New City", Description = "Creates a new city linked to a specific governorate.")]
        public async Task<IActionResult> CreateCity([FromBody] CityCreateDto dto) => Ok(await _adminService.CreateCityAsync(dto));

        [HttpPut("cities/{id}")]
        [SwaggerOperation(Summary = "Update City", Description = "Updates existing city details and its governorate association.")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] CityCreateDto dto) => Ok(await _adminService.UpdateCityAsync(id, dto));

        [HttpDelete("cities/{id}")]
        [SwaggerOperation(Summary = "Delete City", Description = "Permanently removes a city from the system.")]
        public async Task<IActionResult> DeleteCity(int id) => Ok(await _adminService.DeleteCityAsync(id));

        #endregion

        #region ads Endpoints

        [HttpGet("ads")]
        [SwaggerOperation(Summary = "Get All Advertisements", Description = "Retrieves a list of all ads with their creators and owners.")]
        public async Task<IActionResult> GetAds() => Ok(await _adminService.GetAllAdvertisementsAsync());

        [HttpGet("ads/{id}")]
        [SwaggerOperation(Summary = "Get Advertisement by ID", Description = "Retrieves details of a specific advertisement.")]
        public async Task<IActionResult> GetAd(int id) => Ok(await _adminService.GetAdvertisementByIdAsync(id));

        [HttpPost("ads")]
        [SwaggerOperation(Summary = "Add New Advertisement", Description = "Creates a new ad and automatically links it to the logged-in Admin.")]
        public async Task<IActionResult> CreateAd([FromForm] AdvertisementCreateDto dto)
        {
            
            int adminId = User.GetUserId();

            if (adminId == 0)
                return Unauthorized(ResponseDto.FailureResponse("غير مصرح لك، لم يتم العثور على معرف المسؤول في التوكن."));

            var result = await _adminService.CreateAdvertisementAsync(adminId, dto);
            return Ok(result);
        }

        [HttpPut("ads/{id}")]
        [SwaggerOperation(Summary = "Update Advertisement", Description = "Updates ad details and handles image replacement if provided.")]
        public async Task<IActionResult> UpdateAd(int id, [FromForm] AdvertisementUpdateDto dto)
            => Ok(await _adminService.UpdateAdvertisementAsync(id, dto));

        [HttpDelete("ads/{id}")]
        [SwaggerOperation(Summary = "Delete Advertisement", Description = "Permanently removes an advertisement from the database.")]
        public async Task<IActionResult> DeleteAd(int id)
            => Ok(await _adminService.DeleteAdvertisementAsync(id));

        #endregion

        #region Bank Endpoints

        [HttpGet("banks")]
        [SwaggerOperation(Summary = "Get All Banks", Description = "Retrieves a list of all banks registered in the system.")]
        public async Task<IActionResult> GetBanks() => Ok(await _adminService.GetAllBanksAsync());

        [HttpGet("banks/{id}")]
        [SwaggerOperation(Summary = "Get Bank by ID", Description = "Retrieves detailed information about a specific bank using its unique ID.")]
        public async Task<IActionResult> GetBank(int id) => Ok(await _adminService.GetBankByIdAsync(id));

        [HttpPost("banks")]
        [SwaggerOperation(Summary = "Add New Bank", Description = "Creates a new bank record in the system.")]
        public async Task<IActionResult> CreateBank([FromForm] BankCreateDto dto) => Ok(await _adminService.CreateBankAsync(dto));

        [HttpPut("banks/{id}")]
        [SwaggerOperation(Summary = "Update Bank", Description = "Modifies an existing bank's details.")]
        public async Task<IActionResult> UpdateBank(int id, [FromBody] BankUpdateDto dto) => Ok(await _adminService.UpdateBankAsync(id, dto));

        [HttpDelete("banks/{id}")]
        [SwaggerOperation(Summary = "Delete Bank", Description = "Permanently removes a bank from the system. Note: Only banks without linked accounts can be deleted.")]
        public async Task<IActionResult> DeleteBank(int id) => Ok(await _adminService.DeleteBankAsync(id));

        #endregion
    }
}