using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.City;
using Darb.Api.DTOs.Governorate;
using Darb.Api.DTOs.Advertisement;
using Darb.Api.DTOs.Bank;

public interface IAdminService
{
    // Governorate Management
    Task<ResponseDto> GetAllGovernoratesAsync();
    Task<ResponseDto> GetGovernorateByIdAsync(int id);
    Task<ResponseDto> CreateGovernorateAsync(GovernorateCreateDto dto);
    Task<ResponseDto> UpdateGovernorateAsync(int id, GovernorateCreateDto dto);
    Task<ResponseDto> DeleteGovernorateAsync(int id);

    // City Management
    Task<ResponseDto> GetAllCitiesAsync();
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
}