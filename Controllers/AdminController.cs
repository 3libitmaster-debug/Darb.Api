using Darb.Api.DTOs;
using Darb.Api.DTOs.admin;
using Darb.Api.DTOs.admin.Company;
using Darb.Api.DTOs.admin.Customers;
using Darb.Api.DTOs.adminDtos.Advertisement;
using Darb.Api.DTOs.adminDtos.Bank;
using Darb.Api.DTOs.adminDtos.City;
using Darb.Api.DTOs.adminDtos.Governorate;
using Darb.Api.DTOs.Base;
using Darb.Api.Extensions;
using Darb.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace Darb.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService) { _adminService = adminService; }

        #region Dashboard Statistics Endpoints

        [HttpGet("dashboard-stats")]
        [SwaggerOperation(Summary = "Get core dashboard statistics", Description = "Retrieves the four essential metrics for the admin dashboard: pending registrations, pending renewals, total customers, and active advertisements.")]
        public async Task<IActionResult> GetDashboardStats() => Ok(await _adminService.GetDashboardStatsAsync());

        #endregion

        #region Governorate Endpoints

        [HttpGet("governorates")]
        [SwaggerOperation(Summary = "Get all governorates", Description = "Retrieves a comprehensive list of all registered governorates.")]
        public async Task<IActionResult> GetGovs() => Ok(await _adminService.GetAllGovernoratesAsync());

        [HttpGet("governorates/{id:int}")] // 💡 Added :int constraint to prevent route mixing
        [SwaggerOperation(Summary = "Get a governorate by ID", Description = "Retrieves detailed information about a specific governorate using its unique identifier.")]
        public async Task<IActionResult> GetGov(int id) => Ok(await _adminService.GetGovernorateByIdAsync(id));

        [HttpPost("governorates")]
        [SwaggerOperation(Summary = "Create a new governorate", Description = "Registers a new governorate record into the system.")]
        public async Task<IActionResult> CreateGov([FromBody] GovernorateCreateDto dto) => Ok(await _adminService.CreateGovernorateAsync(dto));

        [HttpPut("governorates/{id:int}")]
        [SwaggerOperation(Summary = "Update an existing governorate", Description = "Modifies details of an existing governorate record.")]
        public async Task<IActionResult> UpdateGov(int id, [FromBody] GovernorateCreateDto dto) => Ok(await _adminService.UpdateGovernorateAsync(id, dto));

        [HttpDelete("governorates/{id:int}")]
        [SwaggerOperation(Summary = "Delete a governorate", Description = "Permanently removes a governorate from the system. Action is only permitted if no cities are linked to it.")]
        public async Task<IActionResult> DeleteGov(int id) => Ok(await _adminService.DeleteGovernorateAsync(id));

        #endregion

        #region City Endpoints

        [HttpGet("cities")]
        [SwaggerOperation(Summary = "Get all cities", Description = "Retrieves a list of all registered cities including their parent governorate details.")]
        public async Task<IActionResult> GetCities() => Ok(await _adminService.GetAllCitiesAsync());

        [HttpGet("cities/{id:int}")]
        [SwaggerOperation(Summary = "Get a city by ID", Description = "Retrieves detailed information about a specific city.")]
        public async Task<IActionResult> GetCity(int id) => Ok(await _adminService.GetCityByIdAsync(id));

        // 💡 Moved under 'cities' base path to completely eliminate the collision with 'governorates/{id}'
        [HttpGet("cities/by-governorate/{governorateId:int}")]
        [SwaggerOperation(Summary = "Get all cities within a governorate", Description = "Retrieves all cities belonging to a specified governorate ID.")]
        public async Task<IActionResult> GetCitiesByGov(int governorateId) => Ok(await _adminService.GetCitiesByGovernorateIdAsync(governorateId));

        [HttpPost("cities")]
        [SwaggerOperation(Summary = "Create a new city", Description = "Creates a new city and links it to a valid parent governorate.")]
        public async Task<IActionResult> CreateCity([FromBody] CityCreateDto dto) => Ok(await _adminService.CreateCityAsync(dto));

        [HttpPut("cities/{id:int}")]
        [SwaggerOperation(Summary = "Update an existing city", Description = "Updates an existing city's details and handles its governorate association.")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] CityCreateDto dto) => Ok(await _adminService.UpdateCityAsync(id, dto));

        [HttpDelete("cities/{id:int}")]
        [SwaggerOperation(Summary = "Delete a city", Description = "Permanently removes a specific city from the system.")]
        public async Task<IActionResult> DeleteCity(int id) => Ok(await _adminService.DeleteCityAsync(id));

        #endregion

        #region Advertisement Endpoints

        [HttpGet("ads")]
        [SwaggerOperation(Summary = "Get all advertisements", Description = "Retrieves a complete list of all promotional advertisements including creator and ownership data.")]
        public async Task<IActionResult> GetAds() => Ok(await _adminService.GetAllAdvertisementsAsync());

        [HttpGet("ads/{id:int}")]
        [SwaggerOperation(Summary = "Get an advertisement by ID", Description = "Retrieves specific data and metadata for an advertisement.")]
        public async Task<IActionResult> GetAd(int id) => Ok(await _adminService.GetAdvertisementByIdAsync(id));

        [HttpPost("ads")]
        [SwaggerOperation(Summary = "Create a new advertisement", Description = "Publishes a new advertisement and binds it to the authenticated Admin account extraction.")]
        public async Task<IActionResult> CreateAd([FromForm] AdvertisementCreateDto dto)
        {
            int adminId = User.GetAccountId();

            if (adminId == 0)
                return Unauthorized(ResponseDto.FailureResponse("غير مصرح لك، لم يتم العثور على معرف المسؤول في التوكن."));

            var result = await _adminService.CreateAdvertisementAsync(adminId, dto);
            return Ok(result);
        }

        [HttpPut("ads/{id:int}")]
        [SwaggerOperation(Summary = "Update an existing advertisement", Description = "Updates ad text, criteria, and handles file/image asset replacement if applicable.")]
        public async Task<IActionResult> UpdateAd(int id, [FromForm] AdvertisementUpdateDto dto)
            => Ok(await _adminService.UpdateAdvertisementAsync(id, dto));

        [HttpDelete("ads/{id:int}")]
        [SwaggerOperation(Summary = "Delete an advertisement", Description = "Permanently eliminates an advertisement asset from the persistent storage.")]
        public async Task<IActionResult> DeleteAd(int id)
            => Ok(await _adminService.DeleteAdvertisementAsync(id));

        #endregion

        #region Bank Endpoints

        [HttpGet("banks")]
        [SwaggerOperation(Summary = "Get all banks", Description = "Retrieves a list of all payment gateway banks integrated within the system.")]
        public async Task<IActionResult> GetBanks() => Ok(await _adminService.GetAllBanksAsync());

        [HttpGet("banks/{id:int}")]
        [SwaggerOperation(Summary = "Get a bank by ID", Description = "Retrieves transactional identity information for a specific bank item.")]
        public async Task<IActionResult> GetBank(int id) => Ok(await _adminService.GetBankByIdAsync(id));

        [HttpPost("banks")]
        [SwaggerOperation(Summary = "Create a new bank", Description = "Adds a new supportive financial institution profile.")]
        public async Task<IActionResult> CreateBank([FromForm] BankCreateDto dto) => Ok(await _adminService.CreateBankAsync(dto));

        [HttpPut("banks/{id:int}")]
        [SwaggerOperation(Summary = "Update an existing bank", Description = "Modifies attributes of an operating bank entity.")]
        public async Task<IActionResult> UpdateBank(int id, [FromForm] BankUpdateDto dto) => Ok(await _adminService.UpdateBankAsync(id, dto));

        [HttpDelete("banks/{id:int}")]
        [SwaggerOperation(Summary = "Delete a bank", Description = "Deletes a banking profile. Allowed only if no user accounts or payment slips depend on it.")]
        public async Task<IActionResult> DeleteBank(int id) => Ok(await _adminService.DeleteBankAsync(id));

        #endregion

        #region Customers Management Endpoints

        [HttpGet("customers")]
        [SwaggerOperation(Summary = "Get all customers", Description = "Retrieves a general registry list of all passengers/customers.")]
        public async Task<IActionResult> GetAllCustomers() => Ok(await _adminService.GetAllCustomersAsync());

        [HttpGet("customers/{id:int}")]
        [SwaggerOperation(Summary = "Get a customer by ID", Description = "Fetches a specific customer profile detailing state and contact info.")]
        public async Task<IActionResult> GetCustomerById(int id) => Ok(await _adminService.GetCustomerByIdAsync(id));

        [HttpPost("customers")]
        [SwaggerOperation(Summary = "Create a customer profile", Description = "Manually provisions a new customer user account.")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto dto) => Ok(await _adminService.CreateCustomerAsync(dto));

        [HttpPut("customers/{id:int}")]
        [SwaggerOperation(Summary = "Update a customer profile", Description = "Overwrites specific credential fields of an operating customer account.")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDto dto) => Ok(await _adminService.UpdateCustomerAsync(id, dto));

        [HttpDelete("customers/{id:int}")]
        [SwaggerOperation(Summary = "Delete a customer account", Description = "Performs a deep delete cycle to wipe a customer profile off the records.")]
        public async Task<IActionResult> DeleteCustomer(int id) => Ok(await _adminService.DeleteCustomerAsync(id));

        #endregion

        #region Companies Management Endpoints

        [HttpGet("companies")]
        [SwaggerOperation(Summary = "Get all transport companies", Description = "Fetches all corporate transport operators registered inside the platform.")]
        public async Task<IActionResult> GetAllCompanies() => Ok(await _adminService.GetAllCompaniesAsync());

        [HttpGet("companies/{id:int}")]
        [SwaggerOperation(Summary = "Get a transport company by ID", Description = "Retrieves localized fleet stats and validation metrics of a single transport company.")]
        public async Task<IActionResult> GetCompanyById(int id) => Ok(await _adminService.GetCompanyByIdAsync(id));

        [HttpPost("companies")]
        [SwaggerOperation(Summary = "Create a company profile", Description = "Registers and provisions a new transit vendor profile along with physical binary assets.")]
        public async Task<IActionResult> CreateCompany([FromForm] CompanyCreateDto dto) => Ok(await _adminService.CreateCompanyAsync(dto));

        [HttpPut("companies/{id:int}")]
        [SwaggerOperation(Summary = "Update a company profile", Description = "Modifies active business details and physical credentials for an authorized transportation firm.")]
        public async Task<IActionResult> UpdateCompany(int id, [FromForm] CompanyUpdateDto dto) => Ok(await _adminService.UpdateCompanyAsync(id, dto));

        [HttpDelete("companies/{id:int}")]
        [SwaggerOperation(Summary = "Delete a company account", Description = "Removes a company profile and cascades context deletions if structural relations are clear.")]
        public async Task<IActionResult> DeleteCompany(int id) => Ok(await _adminService.DeleteCompanyAsync(id));

        #endregion

        #region Unified Activation Endpoint

        [HttpPut("toggle-activation/{id}")]
        public async Task<IActionResult> ToggleUserActivation(int id)
        {
            var result = await _adminService.ToggleUserActivationAsync(id);
            return Ok(result);
        }

        #endregion

        #region Subscription & Onboarding Management Endpoints

        [HttpGet("registrations/companies/pending")]
        [SwaggerOperation(Summary = "Get new company registration requests", Description = "Lists all freshly registered corporate entries undergoing initial pipeline validation.")]
        public async Task<IActionResult> GetNewCompanyRegistrationRequests() => Ok(await _adminService.GetNewCompanyRegistrationRequestsAsync());

        [HttpGet("subscriptions/pending")]
        [SwaggerOperation(Summary = "Get pending subscriptions", Description = "Retrieves all processing subscription renewals or payment audit operations.")]
        public async Task<IActionResult> GetPendingSubscriptions() => Ok(await _adminService.GetPendingSubscriptionsAsync());

        [HttpPut("subscriptions/{id:int}/accept")]
        [SwaggerOperation(Summary = "Accept a subscription request", Description = "Approves a pending payment ledger slip, automatically refreshing or shifting active licensing status.")]
        public async Task<IActionResult> AcceptSubscription(int id) => Ok(await _adminService.AcceptSubscriptionAsync(id));

        [HttpPut("subscriptions/{id:int}/reject")]
        [SwaggerOperation(Summary = "Reject a subscription request", Description = "Denies a billing ledger or registration package based on policy defaults.")]
        public async Task<IActionResult> RejectSubscription(int id) => Ok(await _adminService.RejectSubscriptionAsync(id));

        #endregion
    }
}