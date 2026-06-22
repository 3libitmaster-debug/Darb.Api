using Darb.Api.DTOs.admin.Company;
using Darb.Api.DTOs.admin.Complaints;
using Darb.Api.DTOs.admin.Customers;
using Darb.Api.DTOs.adminDtos.Advertisement;
using Darb.Api.DTOs.adminDtos.Bank;
using Darb.Api.DTOs.adminDtos.City;
using Darb.Api.DTOs.adminDtos.Governorate;
using Darb.Api.DTOs.Base;

namespace Darb.Api.Services.Interfaces
{
    public interface IAdminService
    {
        Task<ResponseDto> GetDashboardStatsAsync();

        // Governorate Management
        Task<ResponseDto> GetAllGovernoratesAsync();
        Task<ResponseDto> GetGovernorateByIdAsync(int id);
        Task<ResponseDto> CreateGovernorateAsync(GovernorateCreateDto dto);
        Task<ResponseDto> UpdateGovernorateAsync(int id, GovernorateCreateDto dto);
        Task<ResponseDto> DeleteGovernorateAsync(int id);

        // City Management
        Task<ResponseDto> GetAllCitiesAsync();
        Task<ResponseDto> GetCitiesByGovernorateIdAsync(int governorateId);
        Task<ResponseDto> GetCityByIdAsync(int id);
        Task<ResponseDto> CreateCityAsync(CityCreateDto dto);
        Task<ResponseDto> UpdateCityAsync(int id, CityCreateDto dto);
        Task<ResponseDto> DeleteCityAsync(int id);

        // Advertisement Management
        Task<ResponseDto> GetAllAdvertisementsAsync();
        Task<ResponseDto> GetAdvertisementByIdAsync(int id);
        Task<ResponseDto> CreateAdvertisementAsync(int adminId, AdvertisementCreateDto dto);
        Task<ResponseDto> UpdateAdvertisementAsync(int id, AdvertisementUpdateDto dto);
        Task<ResponseDto> DeleteAdvertisementAsync(int id);

        // Bank Management
        Task<ResponseDto> GetAllBanksAsync();
        Task<ResponseDto> GetBankByIdAsync(int bankId);
        Task<ResponseDto> CreateBankAsync(BankCreateDto dto);
        Task<ResponseDto> UpdateBankAsync(int bankId, BankUpdateDto dto);
        Task<ResponseDto> DeleteBankAsync(int bankId);

        #region Customer Operations
        Task<ResponseDto> GetAllCustomersAsync();
        Task<ResponseDto> GetCustomerByIdAsync(int id);
        Task<ResponseDto> CreateCustomerAsync(CustomerCreateDto dto);
        Task<ResponseDto> UpdateCustomerAsync(int id, CustomerUpdateDto dto);
        Task<ResponseDto> DeleteCustomerAsync(int id);
        #endregion

        #region Company Operations
        Task<ResponseDto> GetAllCompaniesAsync();
        Task<ResponseDto> GetCompanyByIdAsync(int id);
        Task<ResponseDto> CreateCompanyAsync(CompanyCreateDto dto);
        Task<ResponseDto> UpdateCompanyAsync(int id, CompanyUpdateDto dto);
        Task<ResponseDto> DeleteCompanyAsync(int id);
        #endregion

        #region Unified Activation Operation
        // Replaced 4 separate activation/deactivation methods with this single unified function
        Task<ResponseDto> ToggleUserActivationAsync(int userId);
        #endregion


        // Subscription Management
        Task<ResponseDto> GetPendingSubscriptionsAsync();
        Task<ResponseDto> GetNewCompanyRegistrationRequestsAsync();
        Task<ResponseDto> AcceptSubscriptionAsync(int subscriptionId);
        Task<ResponseDto> RejectSubscriptionAsync(int subscriptionId);

        #region Complaints Management
        Task<ResponseDto> GetAllPendingComplaintsAsync();
        Task<ResponseDto> GetComplaintByIdAsync(int complaintId);
        Task<ResponseDto> RespondToComplaintAsync(int complaintId, AdminRespondToComplaintDto dto);
        Task<ResponseDto> DeleteComplaintAsync(int complaintId);
        #endregion
    }
}
