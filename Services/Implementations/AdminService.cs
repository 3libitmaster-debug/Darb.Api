using Microsoft.EntityFrameworkCore;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.City;
using Darb.Api.DTOs.Governorate;
using Darb.Api.DTOs.Advertisement;
using Darb.Api.Models;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Darb.Api.Helpers;
using Microsoft.Extensions.Options;

public class AdminService : IAdminService
{
    private readonly IRepository<Governorate> _govRepo;
    private readonly IRepository<City> _cityRepo;
    private readonly IRepository<Advertisement> _adRepo;
    private readonly IImageService _imageService;
    private readonly ApplicationDbContext _context;
    private readonly string _baseUrl;

    public AdminService(IRepository<Governorate> govRepo, IRepository<City> cityRepo, IRepository<Advertisement> adRepo, IImageService imageService, ApplicationDbContext context, IOptions<ApiSettings> apiOptions)
    {
        _govRepo = govRepo;
        _cityRepo = cityRepo;
        _adRepo = adRepo;
        _imageService = imageService;
        _context = context;
        _baseUrl = apiOptions.Value.BaseUrl ?? string.Empty;
    }

    #region Governorate Logic
    public async Task<ResponseDto> GetAllGovernoratesAsync()
    {
        // Fetching all governorates from the database
        var govs = await _govRepo.GetAllAsync();
        var dtos = govs.Select(g => new GovernorateReadDto { Id = g.GovernorateId, Name = g.Name ?? "N/A" }).ToList();
        return ResponseDto.SuccessResponse($"تم العثور على ({dtos.Count}) محافظة.", dtos);
    }

    public async Task<ResponseDto> GetGovernorateByIdAsync(int id)
    {
        // Fetching a single governorate by its unique identifier
        var gov = await _govRepo.GetByIdAsync(id);
        if (gov == null) return ResponseDto.FailureResponse("المحافظة غير موجودة.");

        return ResponseDto.SuccessResponse("تم استرجاع بيانات المحافظة.", new GovernorateReadDto { Id = gov.GovernorateId, Name = gov.Name ?? "" });
    }

    public async Task<ResponseDto> CreateGovernorateAsync(GovernorateCreateDto dto)
    {
        // Validating and persisting a new governorate record
        var gov = new Governorate { Name = dto.Name };
        await _govRepo.AddAsync(gov);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("تم إضافة المحافظة بنجاح.");
    }

    public async Task<ResponseDto> UpdateGovernorateAsync(int id, GovernorateCreateDto dto)
    {
        // Updating an existing governorate's information
        var gov = await _govRepo.GetByIdAsync(id);
        if (gov == null) return ResponseDto.FailureResponse("المحافظة غير موجودة لتحديثها.");

        gov.Name = dto.Name;
        _govRepo.Update(gov);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("تم تحديث بيانات المحافظة.");
    }

    public async Task<ResponseDto> DeleteGovernorateAsync(int id)
    {
        // Deleting a governorate record
        var gov = await _govRepo.GetByIdAsync(id);
        if (gov == null) return ResponseDto.FailureResponse("المحافظة غير موجودة لحذفها.");

        _govRepo.Delete(gov);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("تم حذف المحافظة بنجاح.");
    }
    #endregion

    #region City Logic
    public async Task<ResponseDto> GetAllCitiesAsync()
    {
        // Fetching cities with their related governorates to display names correctly
        var cities = await _context.Cities.Include(c => c.Governorate).ToListAsync();
        var dtos = cities.Select(c => new CityReadDto
        {
            Id = c.CityId,
            Name = c.Name,
            GovernorateName = c.Governorate?.Name ?? "N/A"
        }).ToList();
        return ResponseDto.SuccessResponse($"تم العثور على ({dtos.Count}) مدينة.", dtos);
    }

    public async Task<ResponseDto> GetCityByIdAsync(int id)
    {
        // Fetching city details including its parent governorate
        var city = await _context.Cities.Include(c => c.Governorate).FirstOrDefaultAsync(c => c.CityId == id);
        if (city == null) return ResponseDto.FailureResponse("المدينة غير موجودة.");

        return ResponseDto.SuccessResponse("تم استرجاع بيانات المدينة.", new CityReadDto
        {
            Id = city.CityId,
            Name = city.Name,
            GovernorateName = city.Governorate?.Name ?? ""
        });
    }

    public async Task<ResponseDto> CreateCityAsync(CityCreateDto dto)
    {
        // Persisting a new city and linking it to a governorate via ID
        var city = new City { Name = dto.Name, GovernorateId = dto.GovernorateId };
        await _cityRepo.AddAsync(city);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("تم إضافة المدينة بنجاح.");
    }

    public async Task<ResponseDto> UpdateCityAsync(int id, CityCreateDto dto)
    {
        // Modifying city details and ensuring the governorate link is updated
        var city = await _cityRepo.GetByIdAsync(id);
        if (city == null) return ResponseDto.FailureResponse("المدينة غير موجودة.");

        city.Name = dto.Name;
        city.GovernorateId = dto.GovernorateId;
        _cityRepo.Update(city);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("تم تحديث المدينة بنجاح.");
    }

    public async Task<ResponseDto> DeleteCityAsync(int id)
    {
        // Removing city record from the database
        var city = await _cityRepo.GetByIdAsync(id);
        if (city == null) return ResponseDto.FailureResponse("المدينة غير موجودة.");

        _cityRepo.Delete(city);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("تم حذف المدينة بنجاح.");
    }
    #endregion

    #region Advertisement Logic

    /// <summary>
    /// Retrieves all advertisements with related user information.
    /// </summary>
    public async Task<ResponseDto> GetAllAdvertisementsAsync()
    {
        // Fetch all advertisement records from the database using the repository
        var ads = await _adRepo.GetAllAsync();

        // Extract unique UserIDs to fetch their emails in a single batch for better performance (Optimization)
        var userIds = ads.Select(a => a.UserId).Distinct().ToList();

        // Fetch user emails and store them in a dictionary for fast lookup
        var users = await _context.Users
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.Email);

        // Map the database entities to Read-Only DTOs for the client side
        var dtos = ads.Select(a => new AdvertisementReadDto
        {
            AdvertisementID = a.AdvertisementID,
            UserID = a.UserId,
            // Safely handle cases where a user might not exist in the dictionary
            User_Email = users.ContainsKey(a.UserId) ? users[a.UserId] : "Unknown User",
            Title = a.Title,
            Description = a.Description,
            ImageUrl = !string.IsNullOrEmpty(a.Image) ? _baseUrl + a.Image : string.Empty,
            StartDateAds = a.StartDateAds,
            EndDateAds = a.EndDateAds,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt
        }).ToList();

        return ResponseDto.SuccessResponse($"Found ({dtos.Count}) advertisements successfully.", dtos);
    }

    /// <summary>
    /// Retrieves a specific advertisement by its unique identifier.
    /// </summary>
    public async Task<ResponseDto> GetAdvertisementByIdAsync(int id)
    {
        // Retrieve the advertisement by ID
        var ad = await _adRepo.GetByIdAsync(id);
        if (ad == null) return ResponseDto.FailureResponse("Advertisement not found.");

        // Fetch the owner/creator information
        var user = await _context.Users.FindAsync(ad.UserId);

        var dto = new AdvertisementReadDto
        {
            AdvertisementID = ad.AdvertisementID,
            UserID = ad.UserId,
            User_Email = user?.Email ?? "Unknown User",
            Title = ad.Title,
            Description = ad.Description,
            ImageUrl = !string.IsNullOrEmpty(ad.Image) ? _baseUrl + ad.Image : string.Empty,
            StartDateAds = ad.StartDateAds,
            EndDateAds = ad.EndDateAds,
            IsActive = ad.IsActive,
            CreatedAt = ad.CreatedAt
        };

        return ResponseDto.SuccessResponse("Advertisement details retrieved successfully.", dto);
    }

    /// <summary>
    /// Creates a new advertisement and links it automatically to the authenticated Admin.
    /// </summary>
    public async Task<ResponseDto> CreateAdvertisementAsync(int adminId, AdvertisementCreateDto dto)
    {
        // Verify if the Admin ID from the token exists in the database to prevent Foreign Key constraints violation
        var adminExists = await _context.Users.AnyAsync(u => u.UserId == adminId);
        if (!adminExists) return ResponseDto.FailureResponse("Unauthorized: Admin user not found.");

        // Initialize the Advertisement entity with data from DTO and the Token
        var ad = new Advertisement
        {
            UserId = adminId,  // Automatic binding to the logged-in admin
            Title = dto.Title,
            Description = dto.Description,
            StartDateAds = dto.StartDateAds,
            EndDateAds = dto.EndDateAds,
            IsActive = dto.IsActive = true, // Default to active if status is null
            CreatedAt = DateHelper.GetYemenTime() // Log the creation time in local timezone
        };

        // Handle image upload via the dedicated ImageService
        if (dto.ImageFile != null)
        {
            var imagePath = await _imageService.SaveImageAsync(dto.ImageFile, "Advertisements");
            if (!string.IsNullOrEmpty(imagePath)) ad.Image = imagePath;
        }

        // Save the new entity to the database
        await _adRepo.AddAsync(ad);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("Advertisement created successfully.");
    }

    /// <summary>
    /// Updates an existing advertisement's details selectively.
    /// </summary>
    public async Task<ResponseDto> UpdateAdvertisementAsync(int id, AdvertisementUpdateDto dto)
    {
        // Fetch the existing record
        var ad = await _adRepo.GetByIdAsync(id);
        if (ad == null) return ResponseDto.FailureResponse("Advertisement not found for update.");

        // Patch-style updates: only update fields that have provided values
        if (!string.IsNullOrEmpty(dto.Title)) ad.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Description)) ad.Description = dto.Description;
        if (dto.StartDateAds.HasValue) ad.StartDateAds = dto.StartDateAds.Value;
        if (dto.EndDateAds.HasValue) ad.EndDateAds = dto.EndDateAds.Value;
        if (dto.IsActive.HasValue) ad.IsActive = dto.IsActive.Value;

        // Process image update via the centralized ImageService method
        if (dto.ImageFile != null)
        {
            var newImagePath = await _imageService.UpdateImageAsync(dto.ImageFile, ad.Image, "Advertisements");
            if (!string.IsNullOrEmpty(newImagePath)) ad.Image = newImagePath;
        }

        // Mark entity as modified and save changes
        _adRepo.Update(ad);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("Advertisement updated successfully.");
    }

    /// <summary>
    /// Deletes an advertisement permanently from the system.
    /// </summary>
    public async Task<ResponseDto> DeleteAdvertisementAsync(int id)
    {
        var ad = await _adRepo.GetByIdAsync(id);
        if (ad == null) return ResponseDto.FailureResponse("Advertisement not found or already deleted.");

        // Delete the image from disk before removing the record
        if (!string.IsNullOrEmpty(ad.Image))
        {
            _imageService.DeleteImage(ad.Image);
        }

        // Remove the record via repository
        _adRepo.Delete(ad);
        await _context.SaveChangesAsync();
        return ResponseDto.SuccessResponse("Advertisement deleted successfully.");
    }

    #endregion


}