using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Passenger;
using Darb.Api.DTOs.Passenger.Darb.Api.DTOs.Passenger;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace Darb.Api.Services.Implementations
{
    public class PassengerService : IPassengerService
    {
        // Injecting Repositories and Context as per your project pattern
        private readonly IRepository<Governorate> _govRepo;
        private readonly IRepository<Advertisement> _adRepo;
        private readonly IRepository<Company> _companyRepo; // Assuming you have a Company Repository
        private readonly ApplicationDbContext _context;

        public PassengerService(
            IRepository<Governorate> govRepo,
            IRepository<Advertisement> adRepo,
            IRepository<Company> companyRepo,
            ApplicationDbContext context)
        {
            _govRepo = govRepo;
            _adRepo = adRepo;
            _companyRepo = companyRepo;
            _context = context;
        }

        /// <summary>
        /// Retrieves the complete Home Page data wrapped in a unified ResponseDto.
        /// </summary>
        public async Task<ResponseDto> GetHomePageDataAsync()
        {
            try
            {
                var homePageData = new HomePageDto();

                // 1. Fetching Active Ads using Repository
                var ads = await _adRepo.GetAllAsync();
                homePageData.AdCards = ads.Where(a => a.IsActive)
                    .Select(a => new AdCardDto
                    {
                        AdvertisementID = a.AdvertisementID,
                        Title = a.Title,
                        Description = a.Description,
                        Image = a.Image
                    }).ToList();

                // 2. Fetching Governorates for the Search Card
                var govs = await _govRepo.GetAllAsync();
                homePageData.SearchCard.Governorates = govs.Select(g => new SimpleGovernorateDto
                {
                    GovernorateId = g.GovernorateId,
                    Name = g.Name ?? ""
                }).ToList();

                // 3. Fetching Companies for the Search Card
                var companies = await _companyRepo.GetAllAsync();
                homePageData.SearchCard.Companies = companies.Select(c => new SimpleCompanyDto
                {
                    CompanyId = c.CompanyId,
                    Name = c.Name ?? "",
                    Logo = c.Logo ?? ""
                }).ToList();

                // 4. Setting Travel Periods.
                homePageData.SearchCard.PeriodOptions = new List<PeriodDto>
                {
                    new PeriodDto { Value = 0, Name = "صباحي" },
                    new PeriodDto { Value = 1, Name = "مسائي" }
                };

                // Returning the standardized success response
                return ResponseDto.SuccessResponse("تم استرجاع بيانات الصفحة الرئيسية بنجاح.", homePageData);
            }
            catch (Exception ex)
            {
                // Handling potential errors
                return ResponseDto.FailureResponse("حدث خطأ أثناء تحميل بيانات الصفحة الرئيسية.");
            }
        }



        public async Task<ResponseDto> SearchTripsAsync(TripSearchQueryDto query)
        {
            try
            {
                // 1. الاستعلام الأساسي: الرحلات المجدولة فقط مع جلب البيانات المرتبطة
                var tripsQuery = _context.Trips
                    .Include(t => t.Company)
                    .Include(t => t.StartGovernate)
                    .Include(t => t.EndGovernate)
                    .Where(t => t.Status == TripStatus.scheduled)
                    .AsQueryable();

                // 2. الفلترة الديناميكية (Dynamic Filtering)
                if (query.FromGovernorateId.HasValue)
                    tripsQuery = tripsQuery.Where(t => t.StartGoveId == query.FromGovernorateId);

                if (query.ToGovernorateId.HasValue)
                    tripsQuery = tripsQuery.Where(t => t.EndGoveId == query.ToGovernorateId);

                if (query.CompanyId.HasValue)
                    tripsQuery = tripsQuery.Where(t => t.CompanyId == query.CompanyId);

                if (query.PeriodValue.HasValue)
                    tripsQuery = tripsQuery.Where(t => (int)t.Period == query.PeriodValue);

                // 3. تحويل النتائج إلى الـ DTO الموحد (TripSearchResultDto)
                var results = await tripsQuery.Select(t => new TripSearchResultDto
                {
                    TripId = t.TripId,
                    CompanyId = t.CompanyId,
                    CompanyName = t.Company != null ? t.Company.Name : "N/A",
                    CompanyLogo = t.Company != null ? t.Company.Logo : "",
                    StartGoveId = t.StartGoveId,
                    StartGoveName = t.StartGovernate != null ? t.StartGovernate.Name : "N/A",
                    EndGoveId = t.EndGoveId,
                    EndGoveName = t.EndGovernate != null ? t.EndGovernate.Name : "N/A",

                    Price = t.BasePrice,

                    DepartureTime = t.DepartureDateTime.ToString("hh:mm tt"),
                    DepartureDate = t.DepartureDateTime.ToString("yyyy-MM-dd"),
                    AvailableSeats = t.AvailableSeats,
                    Period = t.Period.ToString()
                }).ToListAsync();

                return ResponseDto.SuccessResponse($"تم العثور على ({results.Count}) رحلة.", results);
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"خطأ في البحث: {ex.Message}");
            }
        }
    }
}