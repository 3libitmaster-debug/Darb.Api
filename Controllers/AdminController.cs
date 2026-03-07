using Darb.Api.DTOs.Advertisement;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.City;
using Darb.Api.DTOs.Governorate;
using Darb.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Darb.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    [SwaggerTag("Admin Management: Governorate and City configurations")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService) { _adminService = adminService; }

        #region Governorate Endpoints

        [HttpGet("Get-governorates")]
        [SwaggerOperation(Summary = "Get All Governorates", Description = "Retrieves a list of all governorates registered in the system.")]
        public async Task<IActionResult> GetGovs() => Ok(await _adminService.GetAllGovernoratesAsync());

        [HttpGet("governorates/{id}")]
        [SwaggerOperation(Summary = "Get Governorate by ID", Description = "Retrieves detailed information about a specific governorate using its unique ID.")]
        public async Task<IActionResult> GetGov(int id) => Ok(await _adminService.GetGovernorateByIdAsync(id));

        [HttpPost("create-governorate")]
        [SwaggerOperation(Summary = "Add New Governorate", Description = "Creates a new governorate record in the system.")]
        public async Task<IActionResult> CreateGov([FromBody] GovernorateCreateDto dto) => Ok(await _adminService.CreateGovernorateAsync(dto));

        [HttpPut("governorates/{id}")]
        [SwaggerOperation(Summary = "Update Governorate", Description = "Modifies an existing governorate's details.")]
        public async Task<IActionResult> UpdateGov(int id, [FromBody] GovernorateCreateDto dto) => Ok(await _adminService.UpdateGovernorateAsync(id, dto));

        [HttpDelete("governorates/{id}")]
        [SwaggerOperation(Summary = "Delete Governorate", Description = "Permanently removes a governorate from the system. Note: Only governorates without linked cities can be deleted.")]
        public async Task<IActionResult> DeleteGov(int id) => Ok(await _adminService.DeleteGovernorateAsync(id));

        #endregion

        #region City Endpoints

        [HttpGet("Get-cities")]
        [SwaggerOperation(Summary = "Get All Cities", Description = "Retrieves a list of all cities along with their parent governorate information.")]
        public async Task<IActionResult> GetCities() => Ok(await _adminService.GetAllCitiesAsync());

        [HttpGet("cities/{id}")]
        [SwaggerOperation(Summary = "Get City by ID", Description = "Retrieves detailed information about a specific city.")]
        public async Task<IActionResult> GetCity(int id) => Ok(await _adminService.GetCityByIdAsync(id));

        [HttpPost("create-city")]
        [SwaggerOperation(Summary = "Add New City", Description = "Creates a new city linked to a specific governorate.")]
        public async Task<IActionResult> CreateCity([FromBody] CityCreateDto dto) => Ok(await _adminService.CreateCityAsync(dto));

        [HttpPut("cities/{id}")]
        [SwaggerOperation(Summary = "Update City", Description = "Updates existing city details and its governorate association.")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] CityCreateDto dto) => Ok(await _adminService.UpdateCityAsync(id, dto));

        [HttpDelete("cities/{id}")]
        [SwaggerOperation(Summary = "Delete City", Description = "Permanently removes a city from the system.")]
        public async Task<IActionResult> DeleteCity(int id) => Ok(await _adminService.DeleteCityAsync(id));

        #endregion

        #region Advertisement Endpoints

        [HttpGet("advertisements")]
        [SwaggerOperation(Summary = "Get All Advertisements", Description = "Retrieves a list of all ads with their creators and owners.")]
        public async Task<IActionResult> GetAds() => Ok(await _adminService.GetAllAdvertisementsAsync());

        [HttpGet("advertisements/{id}")]
        [SwaggerOperation(Summary = "Get Advertisement by ID", Description = "Retrieves details of a specific advertisement.")]
        public async Task<IActionResult> GetAd(int id) => Ok(await _adminService.GetAdvertisementByIdAsync(id));

        [HttpPost("create-advertisement")]
        [SwaggerOperation(Summary = "Add New Advertisement", Description = "Creates a new ad and automatically links it to the logged-in Admin.")]
        public async Task<IActionResult> CreateAd([FromForm] AdvertisementCreateDto dto)
        {
            
            int adminId = User.GetUserId();

            if (adminId == 0)
                return Unauthorized(ResponseDto.FailureResponse("غير مصرح لك، لم يتم العثور على معرف المسؤول في التوكن."));

            var result = await _adminService.CreateAdvertisementAsync(adminId, dto);
            return Ok(result);
        }

        [HttpPut("advertisements/{id}")]
        [SwaggerOperation(Summary = "Update Advertisement", Description = "Updates ad details and handles image replacement if provided.")]
        public async Task<IActionResult> UpdateAd(int id, [FromForm] AdvertisementUpdateDto dto)
            => Ok(await _adminService.UpdateAdvertisementAsync(id, dto));

        [HttpDelete("advertisements/{id}")]
        [SwaggerOperation(Summary = "Delete Advertisement", Description = "Permanently removes an advertisement from the database.")]
        public async Task<IActionResult> DeleteAd(int id)
            => Ok(await _adminService.DeleteAdvertisementAsync(id));

        #endregion
    }
}