using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.passenger;
using Darb.Api.DTOs.passengerDtos.bookingDtos;
using Darb.Api.DTOs.passengerDtos.homePageDtos;
using Darb.Api.DTOs.passengerDtos.settings;
using Darb.Api.Helpers;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Darb.Api.Services.Implementations
{

    public class PassengerService : IPassengerService
    {
        private readonly IRepository<Governorate> _govRepo;
        private readonly IRepository<Advertisement> _adRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;

        // Base URL for external links and images (injected via Options Pattern)
        private readonly string _baseUrl;

        public PassengerService(
            IRepository<Governorate> govRepo,
            IRepository<Advertisement> adRepo,
            IRepository<Company> companyRepo,
            ApplicationDbContext context,
            IOptions<ApiSettings> apiOptions,
            IImageService imageService)
        {
            _govRepo = govRepo;
            _adRepo = adRepo;
            _companyRepo = companyRepo;
            _context = context;
            _baseUrl = apiOptions.Value.BaseUrl ?? string.Empty;
            _imageService = imageService;
        }

        #region Home Page Data Retrieval Logic
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
                homePageData.AdCards = ads.Where(a => a.AdsStatus == AdsStatus.Active)
                    .Select(a => new AdCardDto
                    {
                        adId = a.AdvertisementID,
                        Title = a.AdsTitle,
                        Description = a.Description,
                        Image = !string.IsNullOrEmpty(a.Image) ? _baseUrl + a.Image : "",
                        StartDate = a.StartDateAds,
                        EndDate = a.EndDateAds,
                        Status = a.AdsStatus,
                        CreatedAt = a.AdsCreatedAt

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

                // 4. DEFINE PERIODS (Morning/Evening)
                homePageData.SearchCard.PeriodOptions = Enum.GetValues(typeof(Periods)).Cast<Periods>()
                 .Select(p => new PeriodDto
                 {
                     PeriodId = (int)p,
                     Name = p.GetType()
                             .GetMember(p.ToString())
                             .FirstOrDefault()
                             ?.GetCustomAttribute<DisplayAttribute>()
                             ?.GetName() ?? p.ToString()
                 }).ToList();

                return ResponseDto.SuccessResponse("Home page data retrieved successfully.", homePageData);
            }
            catch (Exception)
            {
                // Error handling for Home Page loading
                return ResponseDto.FailureResponse("An error occurred while loading home page data.");
            }
        }
        #endregion

        #region Trip Search Logic
        /// <summary>
        /// Searches for scheduled trips based on dynamic Account filters.
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
                    .Where(t => t.TripStatus == TripStatus.scheduled)
                    .AsQueryable();

                // 2. DYNAMIC FILTERING LOGIC
                // Filters are only applied if the Account provides a value (> 0).

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
                if (query.PeriodId.HasValue && query.PeriodId > 0)
                    tripsQuery = tripsQuery.Where(t => (int)t.Period == query.PeriodId);

                // Filter by Trip Date (Ignores time part for strict date matching)
                if (query.Date.HasValue && query.Date.Value.Year > 2000)
                {
                    var searchDate = query.Date.Value.Date;
                    tripsQuery = tripsQuery.Where(t => t.DepDate.Date == searchDate);
                }

                // 3. DATA PROJECTION & MAPPING
                // Convert database entities to TripSearchResultDto with formatted strings.
                var results = await tripsQuery.Select(t => new TripSearchResultDto
                {
                    TripId = t.TripId,
                    CompanyId = t.CompanyId,
                    CompanyName = t.Company != null ? (t.Company.Name ?? "N/A") : "N/A",
                    // Ensure the logo path is absolute by adding the BaseUrl
                    CompanyLogo = (t.Company != null && !string.IsNullOrEmpty(t.Company.Logo))
                                  ? _baseUrl + t.Company.Logo : "",
                    CompanyRating = t.Company != null ? t.Company.AverageRating : 0.0,
                    StartGoveId = t.StartGoveId,
                    StartGoveName = t.StartGovernate != null ? (t.StartGovernate.Name ?? "N/A") : "N/A",
                    EndGoveId = t.EndGoveId,
                    EndGoveName = t.EndGovernate != null ? (t.EndGovernate.Name ?? "N/A") : "N/A",
                    BasePrice = t.Price,
                    DepartureDate = t.DepDate.ToString("yyyy-MM-dd"),
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
        #endregion

        #region Company Stations Retrieval Logic
        /// <summary>
        /// Retrieves all stations for a specific trip by its TripId.
        /// </summary>
        /// <param name="tripId">The ID of the trip.</param>
        /// <returns>A ResponseDto containing a list of TripScheduleResponseDto.</returns>
        public async Task<ResponseDto> GetTripStationsAsync(int tripId)
        {
            try
            {
                var stations = await _context.TripSchedules
                    .Include(tr => tr.Station)
                        .ThenInclude(s => s!.City)
                    .Where(tr => tr.TripId == tripId)
                    .Select(tr => new TripScheduleResponseDto
                    {
                        TripScheduleId = tr.TripScheduleId,
                        TripId = tr.TripId,
                        StationId = tr.StationId,
                        DepartureTime = tr.DepartureTime.ToString("hh:mm tt"),
                        CityName = tr.Station != null && tr.Station.City != null ? (tr.Station.City.Name ?? string.Empty) : string.Empty,
                        Address = tr.Station != null ? (tr.Station.Address ?? string.Empty) : string.Empty,
                        SeatFare = tr.SeatFare
                    }).ToListAsync();

                if (stations.Count == 0)
                    return ResponseDto.SuccessResponse("No stations found for the specified company and governorate.", stations);

                return ResponseDto.SuccessResponse($"Retrieved {stations.Count} stations successfully.", stations);
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"Failed to retrieve stations: {ex.Message}");
            }
        }
        #endregion

        #region Company Bank Accounts Retrieval Logic
        public async Task<ResponseDto> GetCompanyBankAccountsAsync(int companyId)
        {
            var bankAccounts = await _context.BankAccounts
                .Include(ba => ba.Bank)
                .Where(ba => ba.CompanyId == companyId)
                .Select(ba => new Darb.Api.DTOs.passengerDtos.bookingDtos.BankAccountsDropDownListDto
                {
                    BankAccountId = ba.BankAccountId,
                    BankName = ba.Bank != null ? (ba.Bank.BankName ?? "غير متوفر") : "غير متوفر",
                    AccountNumber = ba.AccountNumber,
                    AccountHolderName = ba.HolderName,
                    LogoUrl = !string.IsNullOrEmpty(ba.Bank!.LogoUrl) ? _baseUrl + ba.Bank.LogoUrl : string.Empty
                })
                .ToListAsync();

            return ResponseDto.SuccessResponse($"تم استرجاع حسابات الشركة البنكية بنجاح. ({bankAccounts.Count})", bankAccounts);
        }
        #endregion

        #region Book Trip Logic
        /// <summary>
        /// Orchestrates the booking process. 
        /// Handles seat inventory, multi-passenger registration, and owner-as-passenger injection.
        /// </summary>
        public async Task<ResponseDto> BookTripAsync(int passengerId, BookingRequestDto request)
        {
            // 1. PRE-TRANSACTION VALIDATIONS ---

            // Validate the existence of the TripSchedule and the associated Trip
            var tripSchedule = await _context.TripSchedules
                .Include(tr => tr.Trip)
                .FirstOrDefaultAsync(tr => tr.TripScheduleId == request.TripScheduleId);

            if (tripSchedule == null || tripSchedule.Trip == null)
                return ResponseDto.FailureResponse("مسار الرحلة المختار غير متاح حالياً.");

            var trip = tripSchedule.Trip;

            // Ensure the trip is still open for booking
            if (trip.TripStatus != TripStatus.scheduled)
                return ResponseDto.FailureResponse("عذراً، هذه الرحلة لم تعد متاحة للحجز.");

            // Calculate total seats required from the provided passengers list
            int totalSeatsRequired = request.AdditionalPassengers?.Count ?? 0;

            if (totalSeatsRequired == 0)
                return ResponseDto.FailureResponse("يجب إضافة راكب واحد على الأقل.");

            // Verify physical seat availability in the bus
            if (trip.AvailableSeats < totalSeatsRequired)
                return ResponseDto.FailureResponse($"عذراً، لا توجد مقاعد كافية. المقاعد المتاحة: {trip.AvailableSeats}");

            // Calculate total financial amount based on route fare
            decimal totalAmount = tripSchedule.SeatFare * totalSeatsRequired;

            // --- 2. EXECUTION STRATEGY (Resiliency) ---
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // A. Create Booking Header
                    var booking = new Booking
                    {
                        PassengerId = passengerId,
                        TripScheduleId = tripSchedule.TripScheduleId,
                        ReservedSeatsCount = totalSeatsRequired,
                        TotalAmount = totalAmount,
                        Status = BookingStatus.PendingAttachment, // Phase 1: Waiting for receipt upload
                        BookingAt = DateHelper.GetYemenTime()
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync(); // Commit to generate BookingId for FK relations

                    // B. Build Unified Passenger List from DTOs
                    var allPassengersList = new List<PassengerDetails>();

                    // Map all passengers from the request
                    foreach (var pDto in request.AdditionalPassengers)
                    {
                        allPassengersList.Add(new PassengerDetails
                        {
                            BookingId = booking.BookingId,
                            FullName = pDto.FullName,
                            NationalId = pDto.NationalId,
                            PhoneNumber = pDto.PhoneNumber,
                            BirthDate = pDto.BirthDate,
                        });
                    }

                    // Bulk save all passengers to optimize database performance
                    _context.PassengerDetails.AddRange(allPassengersList);
                    await _context.SaveChangesAsync();

                    // C. Generate Placeholder E-Ticket for the booking
                    _context.ETickets.Add(new ETicket
                    {
                        BookingId = booking.BookingId,
                        TicketCode = null, // Generated by admin later
                        Status = ETicketStatus.UnValid
                    });

                    // D. Inventory Management: Deduct seats and update trip status if full
                    trip.AvailableSeats -= totalSeatsRequired;
                    if (trip.AvailableSeats == 0)
                        trip.TripStatus = TripStatus.Fulled;

                    _context.Trips.Update(trip);
                    await _context.SaveChangesAsync();

                    // Finalize the transaction
                    await transaction.CommitAsync();

                    return ResponseDto.SuccessResponse("تم إنشاء الحجز المبدئي بنجاح. يرجى رفع صورة السند لتأكيد الحجز.", booking.BookingId);
                }
                catch (Exception ex)
                {
                    // Rollback all changes in case of failure to maintain data integrity
                    await transaction.RollbackAsync();
                    return ResponseDto.FailureResponse($"فشلت عملية الحجز: {ex.Message}");
                }
            });
        }
        #endregion

        #region Upload Booking Payment Receipts Logic
        public async Task<ResponseDto> UploadReceiptAsync(int passengerId, UploadReceiptDto request)
        {
            var passenger = await _context.Passengers.FirstOrDefaultAsync(p => p.PassengerId == passengerId);
            if (passenger == null)
                return ResponseDto.FailureResponse("Passenger profile not found.");

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == request.BookingId && b.PassengerId == passenger.PassengerId);

            if (booking == null)
                return ResponseDto.FailureResponse("Booking not found or does not belong to you.");

            if (booking.Status != BookingStatus.PendingAttachment)
                return ResponseDto.FailureResponse("This booking is no longer pending receipt upload.");

            if (request.ReceiptImage == null)
                return ResponseDto.FailureResponse("Receipt image is required.");

            string? receiptImagePath = await _imageService.SaveImageAsync(request.ReceiptImage, "Booking Payment Receipts");

            if (string.IsNullOrEmpty(receiptImagePath))
                return ResponseDto.FailureResponse("Failed to upload the payment receipt.");

            try
            {
                booking.ReceiptImagePath = receiptImagePath;
                _context.Bookings.Update(booking);
                booking.Status = BookingStatus.AwaitingConfirmation;
                await _context.SaveChangesAsync();

                return ResponseDto.SuccessResponse("Booking successful and receipt uploaded. Waiting for admin confirmation.", booking.BookingId);
            }
            catch (Exception ex)
            {

                _imageService.DeleteImage(receiptImagePath);
                return ResponseDto.FailureResponse($"System Error updating booking: {ex.Message}");
            }
        }
        #endregion

        #region Passenger Profile Logic
        public async Task<ResponseDto> GetProfileAsync(int passengerId)
        {
            try
            {
                var profile = await _context.Passengers
                    .Include(p => p.Account)
                    .Where(p => p.PassengerId == passengerId)
                    .Select(p => new PassengerProfileDto
                    {
                        PassengerId = p.PassengerId,
                        FullName = p.FullName ?? "",
                        DateOfBirth = p.DateOfBirth,
                        PhoneNumber = p.Phone ?? "",
                        Address = p.Address ?? "",
                        NationalId = p.NationalId ?? "",
                        Email = p.Account != null ? p.Account.Email ?? "" : "",
                        Password = p.Account != null ? p.Account.Password ?? "" : "",
                        CreatedAt = p.Account != null ? p.Account.JoinDate : DateTime.MinValue
                    })
                    .FirstOrDefaultAsync();

                if (profile == null)
                    return ResponseDto.FailureResponse("لم يتم العثور على بيانات الحساب الشخصي.");

                return ResponseDto.SuccessResponse("تم استرجاع بيانات الحساب الشخصي بنجاح.", profile);
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"فشل استرجاع بيانات الحساب الشخصي: {ex.Message}");
            }
        }
        #endregion

        #region My Bookings Retrieval Logic
        public async Task<ResponseDto> GetMyBookingsAsync(int passengerId)
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.TripSchedule)
                        .ThenInclude(ts => ts!.Trip)
                            .ThenInclude(t => t!.Company)
                    .Include(b => b.TripSchedule)
                        .ThenInclude(ts => ts!.Trip)
                            .ThenInclude(t => t!.StartGovernate)
                    .Include(b => b.TripSchedule)
                        .ThenInclude(ts => ts!.Trip)
                            .ThenInclude(t => t!.EndGovernate)
                    .Include(b => b.ETicket)
                    .Where(b => b.PassengerId == passengerId)
                    .OrderByDescending(b => b.BookingAt)
                    .Select(b => new MyBookingDto
                    {
                        BookingId = b.BookingId,
                        BookingStatus = (int)b.Status,
                        BookingAt = b.BookingAt,
                        TotalAmount = b.TotalAmount,
                        ReservedSeatsCount = b.ReservedSeatsCount,

                        TripScheduleId = b.TripScheduleId,
                        TripId = b.TripSchedule != null ? b.TripSchedule.TripId : 0,
                        StartGovernorate = b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.StartGovernate != null ? (b.TripSchedule.Trip.StartGovernate.Name ?? "غير متوفر") : "غير متوفر",
                        EndGovernorate = b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.EndGovernate != null ? (b.TripSchedule.Trip.EndGovernate.Name ?? "غير متوفر") : "غير متوفر",
                        DepartureDate = b.TripSchedule != null && b.TripSchedule.Trip != null ? b.TripSchedule.Trip.DepDate.ToString("yyyy-MM-dd") : string.Empty,
                        DepartureTime = b.TripSchedule != null ? b.TripSchedule.DepartureTime.ToString("hh:mm tt") : string.Empty,

                        CompanyId = b.TripSchedule != null && b.TripSchedule.Trip != null ? b.TripSchedule.Trip.CompanyId : 0,
                        CompanyName = b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.Company != null ? (b.TripSchedule.Trip.Company.Name ?? "غير متوفر") : "غير متوفر",
                        CompanyLogo = b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.Company != null && !string.IsNullOrEmpty(b.TripSchedule.Trip.Company.Logo) ? _baseUrl + b.TripSchedule.Trip.Company.Logo : string.Empty,

                        TicketCode = b.ETicket.TicketCode,
                        TicketStatus = (int)b.ETicket.Status
                    })
                    .ToListAsync();

                if (bookings.Count == 0)
                    return ResponseDto.SuccessResponse("لا يوجد حجوزات سابقة.", bookings);

                return ResponseDto.SuccessResponse($"تم استرجاع الحجوزات بنجاح. ({bookings.Count})", bookings);
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"فشل استرجاع الحجوزات: {ex.Message}");
            }
        }
        #endregion

        #region CRUD Reviews Logic (Refactored with Specific DTOs)

        /// <summary>
        /// Retrieves a specific review by its unique ID and returns it as a ReviewReturnDto.
        /// </summary>
        public async Task<ResponseDto> GetReviewByIdAsync(int reviewId)
        {
            try
            {
                var review = await _context.Review
                    .Where(r => r.ReviewId == reviewId)
                    .Select(r => new ReviewResponseDto // Updated naming
                    {
                        ReviewId = r.ReviewId,
                        Rating = r.Rating,
                        Description = r.Description,
                        Date = r.ReviewDate.ToString("yyyy-MM-dd")
                    })
                    .FirstOrDefaultAsync();

                if (review == null)
                    return ResponseDto.FailureResponse("المراجعة غير موجودة");

                return new ResponseDto { Success = true, Message = "Success", Data = review };
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a new review using AddReviewDto and updates the company's average rating.
        /// </summary>
        public async Task<ResponseDto> AddReviewAsync(int passengerId, AddReviewDto request)
        {
            try
            {
                var company = await _context.Companies.FindAsync(request.CompanyId);
                if (company == null) return ResponseDto.FailureResponse("الشركة غير موجودة");

                var review = new Review
                {
                    PassengerId = passengerId,
                    CompanyId = request.CompanyId,
                    Rating = request.Rating,
                    Description = request.Description,
                    ReviewDate = DateHelper.GetYemenTime()
                };

                _context.Review.Add(review);
                await _context.SaveChangesAsync();

                // Ensure the company rating is updated after insertion
                await RecalculateCompanyRating(request.CompanyId);

                return ResponseDto.SuccessResponse("تم إضافة تقييمك بنجاح");
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"Error adding review: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all reviews for a passenger using ReviewReturnDto.
        /// </summary>
        public async Task<ResponseDto> GetPassengerReviewsAsync(int passengerId)
        {
            try
            {
                var reviews = await _context.Review
                    .Where(r => r.PassengerId == passengerId)
                    .Select(r => new ReviewResponseDto // Updated naming
                    {
                        ReviewId = r.ReviewId,
                        Rating = r.Rating,
                        Description = r.Description,
                        Date = r.ReviewDate.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                return ResponseDto.SuccessResponse(data: reviews, message: "Success");
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"Error fetching reviews: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing review using UpdateReviewDto and recalculates the rating.
        /// </summary>
        public async Task<ResponseDto> UpdateReviewAsync(int passengerId, int reviewId, UpdateReviewDto request)
        {
            try
            {
                var review = await _context.Review
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.PassengerId == passengerId);

                if (review == null) return ResponseDto.FailureResponse("المراجعة غير موجودة أو لا تملك صلاحية تعديلها");

                review.Rating = request.Rating;
                review.Description = request.Description;
                review.ReviewDate = DateHelper.GetYemenTime();

                _context.Review.Update(review);
                await _context.SaveChangesAsync();

                // Recalculate to reflect the updated score
                await RecalculateCompanyRating(review.CompanyId);

                return ResponseDto.SuccessResponse("تم تحديث التقييم بنجاح");
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"Error updating review: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a review and updates the company's average rating accordingly.
        /// </summary>
        public async Task<ResponseDto> DeleteReviewAsync(int passengerId, int reviewId)
        {
            try
            {
                var review = await _context.Review
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.PassengerId == passengerId);

                if (review == null) return ResponseDto.FailureResponse("المراجعة غير موجودة");

                int companyId = review.CompanyId;

                _context.Review.Remove(review);
                await _context.SaveChangesAsync();

                // Recalculate after deletion to ensure accuracy
                await RecalculateCompanyRating(companyId);

                return ResponseDto.SuccessResponse("تم حذف التقييم بنجاح");
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"Error deleting review: {ex.Message}");
            }
        }

        /// <summary>
        /// Private helper to maintain data integrity for company average ratings.
        /// </summary>
        private async Task RecalculateCompanyRating(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company != null)
            {
                var ratings = await _context.Review
                    .Where(r => r.CompanyId == companyId)
                    .Select(r => r.Rating)
                    .ToListAsync();

                company.AverageRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0;

                _context.Companies.Update(company);
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}