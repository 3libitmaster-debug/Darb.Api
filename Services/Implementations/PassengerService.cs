using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Passenger;
using Darb.Api.DTOs.Station;
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
                homePageData.AdCards = ads.Where(a => a.IsActive == true)
                    .Select(a => new AdCardDto
                    {
                        AdvertisementID = a.AdvertisementID,
                        Title = a.Title,
                        Description = a.Description,
                        Image = !string.IsNullOrEmpty(a.Image) ? _baseUrl + a.Image : "",
                        StartDate = a.StartDateAds,
                        EndDate = a.EndDateAds,
                        IsActive = a.IsActive,
                        CreatedAt = a.CreatedAt

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
                     Value = (int)p,
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
                    tripsQuery = tripsQuery.Where(t => t.DepartureDate.Date == searchDate);
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
                    StartGoveId = t.StartGoveId,
                    StartGoveName = t.StartGovernate != null ? (t.StartGovernate.Name ?? "N/A") : "N/A",
                    EndGoveId = t.EndGoveId,
                    EndGoveName = t.EndGovernate != null ? (t.EndGovernate.Name ?? "N/A") : "N/A",
                    Price = t.BasePrice,
                    DepartureTime = t.DepartureDate.ToString("hh:mm tt"), // We will just display the general date/time or leave it since it's Date now? Actually if it's Date only, DepartureTime is empty. Let's just output ""
                    DepartureDate = t.DepartureDate.ToString("yyyy-MM-dd"),
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
        /// <returns>A ResponseDto containing a list of TripRouteResponseDto.</returns>
        public async Task<ResponseDto> GetTripStationsAsync(int tripId)
        {
            try
            {
                var stations = await _context.TripRoutes
                    .Include(tr => tr.Station)
                        .ThenInclude(s => s!.City)
                    .Where(tr => tr.TripId == tripId)
                    .Select(tr => new TripRouteResponseDto
                    {
                        TripRouteId = tr.TripRouteId,
                        TripId = tr.TripId,
                        StationId = tr.StationId,
                        DepartureTime = tr.DepartureTime.ToString(@"hh\:mm"),
                        CityName = tr.Station != null && tr.Station.City != null ? (tr.Station.City.Name ?? string.Empty) : string.Empty,
                        Address = tr.Station != null ? (tr.Station.Address ?? string.Empty) : string.Empty,
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
                .Select(ba => new Darb.Api.DTOs.BankAccount.BankAccountsDropDownListDto
                {
                    BankAccountId = ba.BankAccountId,
                    BankName = ba.Bank != null ? (ba.Bank.BankName ?? "غير متوفر") : "غير متوفر",
                    AccountNumber = ba.AccountNumber,
                    AccountHolderName = ba.AccountHolderName,
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
            // --- 1. PRE-TRANSACTION VALIDATIONS ---

            // Fetch the account owner's profile (The person making the booking)
            var passengerProfile = await _context.Passengers
                .FirstOrDefaultAsync(p => p.PassengerId == passengerId);

            if (passengerProfile == null)
                return ResponseDto.FailureResponse("عذراً، لم يتم العثور على ملف تعريف المستخدم.");

            // Validate the existence of the TripRoute and the associated Trip
            var tripRoute = await _context.TripRoutes
                .Include(tr => tr.Trip)
                .FirstOrDefaultAsync(tr => tr.TripRouteId == request.TripRouteId);

            if (tripRoute == null || tripRoute.Trip == null)
                return ResponseDto.FailureResponse("مسار الرحلة المختار غير متاح حالياً.");

            var trip = tripRoute.Trip;

            // Ensure the trip is still open for booking
            if (trip.Status != TripStatus.scheduled)
                return ResponseDto.FailureResponse("عذراً، هذه الرحلة لم تعد متاحة للحجز.");

            // Calculate total seats required: (1 if Owner is traveling) + (count of additional passengers)
            int additionalCount = request.AdditionalPassengers?.Count ?? 0;
            int totalSeatsRequired = additionalCount + (request.IsOwnerPassenger ? 1 : 0);

            if (totalSeatsRequired == 0)
                return ResponseDto.FailureResponse("يجب إضافة راكب واحد على الأقل (سواء صاحب الحساب أو مرافق).");

            if (totalSeatsRequired > 10)
                return ResponseDto.FailureResponse("لا يمكن حجز أكثر من 10 مقاعد في عملية واحدة.");

            // Verify physical seat availability in the bus
            if (trip.AvailableSeats < totalSeatsRequired)
                return ResponseDto.FailureResponse($"عذراً، لا توجد مقاعد كافية. المقاعد المتاحة: {trip.AvailableSeats}");

            // Calculate total financial amount based on route fare
            decimal totalAmount = tripRoute.RouteFare * totalSeatsRequired;

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
                        PassengerId = passengerProfile.PassengerId,
                        TripRouteId = tripRoute.TripRouteId,
                        NumberOfSeats = totalSeatsRequired,
                        TotalAmount = totalAmount,
                        Status = BookingStatus.PendingAttachment, // Phase 1: Waiting for receipt upload
                        BookingAt = DateHelper.GetYemenTime()
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync(); // Commit to generate BookingId for FK relations

                    // B. Build Unified Passenger List (Mapping Profile & DTOs)
                    var allPassengersList = new List<PassengerDetails>();

                    // Logic: If IsOwnerPassenger is true, "Pull" owner's profile data into PassengerDetails
                    if (request.IsOwnerPassenger)
                    {
                        allPassengersList.Add(new PassengerDetails
                        {
                            BookingId = booking.BookingId,
                            FullName = passengerProfile.FullName ?? "",
                            NationalId = passengerProfile.NationalId ?? "",
                            PhoneNumber = passengerProfile.Phone ?? "",
                            BirthDate = passengerProfile.DateOfBirth,
                            Address = passengerProfile.Address
                        });
                    }

                    // Append additional companions from the request
                    if (additionalCount > 0)
                    {
                        foreach (var pDto in request.AdditionalPassengers!)
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
                    }

                    // Bulk save all passengers to optimize database performance
                    _context.PassengerDetails.AddRange(allPassengersList);
                    await _context.SaveChangesAsync();

                    // C. Generate Placeholder E-Tickets for each passenger
                    foreach (var pDetail in allPassengersList)
                    {
                        _context.ETickets.Add(new ETicket
                        {
                            PassengerDetailId = pDetail.PassengerDetailsId,
                            TicketCode = null, // Generated by admin later
                            Status = ETicketStatus.Active,
                            IsConfirmed = false
                        });
                    }

                    // D. Inventory Management: Deduct seats and update trip status if full
                    trip.AvailableSeats -= totalSeatsRequired;
                    if (trip.AvailableSeats == 0)
                        trip.Status = TripStatus.Fulled;

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
                    .Include(p => p.User)
                    .Where(p => p.PassengerId == passengerId)
                    .Select(p => new PassengerProfileDto
                    {
                        PassengerId = p.PassengerId,
                        FullName = p.FullName ?? "",
                        DateOfBirth = p.DateOfBirth,
                        PhoneNumber = p.Phone ?? "",
                        Address = p.Address ?? "",
                        NationalId = p.NationalId ?? "",
                        Email = p.User != null ? p.User.Email ?? "" : "",
                        Password = p.User != null ? p.User.Password ?? "" : "",
                        CreatedAt = p.User != null ? p.User.JoinDate : DateTime.MinValue
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
    }
}