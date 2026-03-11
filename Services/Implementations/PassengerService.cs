using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Passenger;
using Darb.Api.DTOs.Passenger.Darb.Api.DTOs.Passenger;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Darb.Api.Services.Implementations
{
    /// <summary>
    /// Service implementation for Passenger-related operations including 
    /// Home Page data retrieval and Trip Searching.
    /// </summary>
    public class PassengerService : IPassengerService
    {
        // Dependency Injection: Repositories and Context
        private readonly IRepository<Governorate> _govRepo;
        private readonly IRepository<Advertisement> _adRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly ApplicationDbContext _context;

        // Base URL for external links and images (injected via Options Pattern)
        private readonly string _baseUrl;

        public PassengerService(
            IRepository<Governorate> govRepo,
            IRepository<Advertisement> adRepo,
            IRepository<Company> companyRepo,
            ApplicationDbContext context,
            IOptions<ApiSettings> apiOptions)
        {
            _govRepo = govRepo;
            _adRepo = adRepo;
            _companyRepo = companyRepo;
            _context = context;
            _baseUrl = apiOptions.Value.BaseUrl;
        }

        /// <summary>
        /// Fetches all necessary data for the Mobile Home Page (Ads, Governorates, and Companies).
        /// </summary>
        /// <returns>A unified ResponseDto containing the HomePageDto.</returns>
        public async Task<ResponseDto> GetHomePageDataAsync()
        {
            try
            {
                var homePageData = new HomePageDto();

                // 1. FETCH ACTIVE ADVERTISEMENTS
                // Maps advertisements to AdCardDto and appends the Base Server URL to images.
                var ads = await _adRepo.GetAllAsync();
                homePageData.AdCards = ads.Where(a => a.IsActive)
                    .Select(a => new AdCardDto
                    {
                        AdvertisementID = a.AdvertisementID,
                        Title = a.Title,
                        Description = a.Description,
                        Image = !string.IsNullOrEmpty(a.Image) ? _baseUrl + a.Image : ""
                    }).ToList();

                // 2. FETCH GOVERNORATES FOR SEARCH FILTERS
                var govs = await _govRepo.GetAllAsync();
                homePageData.SearchCard.Governorates = govs.Select(g => new SimpleGovernorateDto
                {
                    GovernorateId = g.GovernorateId,
                    Name = g.Name ?? ""
                }).ToList();

                // 3. FETCH COMPANIES FOR SEARCH FILTERS
                // Appends the Base Server URL to the company logos for mobile display.
                var companies = await _companyRepo.GetAllAsync();
                homePageData.SearchCard.Companies = companies.Select(c => new SimpleCompanyDto
                {
                    CompanyId = c.CompanyId,
                    Name = c.Name,
                    Logo = !string.IsNullOrEmpty(c.Logo) ? _baseUrl + c.Logo : ""
                }).ToList();

                // 4. DEFINE STATIC TRAVEL PERIODS (Morning/Evening)
                homePageData.SearchCard.PeriodOptions = new List<PeriodDto>
                {
                    new PeriodDto { Value = 0, Name = "صباحي" },
                    new PeriodDto { Value = 1, Name = "مسائي" }
                };

                return ResponseDto.SuccessResponse("Home page data retrieved successfully.", homePageData);
            }
            catch (Exception ex)
            {
                // Error handling for Home Page loading
                return ResponseDto.FailureResponse("An error occurred while loading home page data.");
            }
        }

        /// <summary>
        /// Searches for scheduled trips based on dynamic user filters.
        /// </summary>
        /// <param name="query">DTO containing filter parameters like Date, Period, and Governorates.</param>
        /// <returns>A list of matching trips wrapped in a ResponseDto.</returns>
        public async Task<ResponseDto> SearchTripsAsync(TripSearchQueryDto query)
        {
            try
            {
                // 1. INITIAL QUERY SETUP
                // Fetch only 'Scheduled' trips and include related entities using Eager Loading.
                var tripsQuery = _context.Trips
                    .Include(t => t.Company)
                    .Include(t => t.StartGovernate)
                    .Include(t => t.EndGovernate)
                    .Where(t => t.Status == TripStatus.scheduled)
                    .AsQueryable();

                // 2. DYNAMIC FILTERING LOGIC
                // Filters are only applied if the user provides a value (> 0).

                // Filter by Departure Governorate
                if (query.FromGovernorateId.HasValue && query.FromGovernorateId > 0)
                    tripsQuery = tripsQuery.Where(t => t.StartGoveId == query.FromGovernorateId);

                // Filter by Destination Governorate
                if (query.ToGovernorateId.HasValue && query.ToGovernorateId > 0)
                    tripsQuery = tripsQuery.Where(t => t.EndGoveId == query.ToGovernorateId);

                // Filter by Specific Transport Company
                if (query.CompanyId.HasValue && query.CompanyId > 0)
                    tripsQuery = tripsQuery.Where(t => t.CompanyId == query.CompanyId);

                // Filter by Trip Period (Morning/Evening)
                if (query.PeriodValue.HasValue && query.PeriodValue > 0)
                    tripsQuery = tripsQuery.Where(t => (int)t.Period == query.PeriodValue);

                // Filter by Trip Date (Ignores time part for strict date matching)
                if (query.Date.HasValue && query.Date.Value.Year > 2000)
                {
                    var searchDate = query.Date.Value.Date;
                    tripsQuery = tripsQuery.Where(t => t.DepartureDateTime.Date == searchDate);
                }

                // 3. DATA PROJECTION & MAPPING
                // Convert database entities to TripSearchResultDto with formatted strings.
                var results = await tripsQuery.Select(t => new TripSearchResultDto
                {
                    TripId = t.TripId,
                    CompanyId = t.CompanyId,
                    CompanyName = t.Company != null ? t.Company.Name : "N/A",
                    // Ensure the logo path is absolute by adding the BaseUrl
                    CompanyLogo = (t.Company != null && !string.IsNullOrEmpty(t.Company.Logo))
                                  ? _baseUrl + t.Company.Logo : "",
                    StartGoveId = t.StartGoveId,
                    StartGoveName = t.StartGovernate != null ? t.StartGovernate.Name : "N/A",
                    EndGoveId = t.EndGoveId,
                    EndGoveName = t.EndGovernate != null ? t.EndGovernate.Name : "N/A",
                    Price = t.BasePrice,
                    DepartureTime = t.DepartureDateTime.ToString("hh:mm tt"), // 12-hour format
                    DepartureDate = t.DepartureDateTime.ToString("yyyy-MM-dd"),
                    AvailableSeats = t.AvailableSeats,
                    Period = t.Period.ToString()
                }).ToListAsync();

                // 4. FINAL RESPONSE HANDLING
                if (results.Count == 0)
                    return ResponseDto.SuccessResponse("No trips found matching your search criteria.", results);

                return ResponseDto.SuccessResponse($"Found {results.Count} trips successfully.", results);
            }
            catch (Exception ex)
            {
                // Global exception handling for search operation
                return ResponseDto.FailureResponse($"Search failed due to an error: {ex.Message}");
            }
        }
    }
}