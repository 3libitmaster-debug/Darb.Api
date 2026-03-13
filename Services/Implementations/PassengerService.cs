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
            _baseUrl = apiOptions.Value.BaseUrl;
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
                catch (Exception ex)
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
        #endregion

        #region Company Stations Retrieval Logic
        /// <summary>
        /// Retrieves all stations for a specific company within a specific governorate.
        /// </summary>
        /// <param name="companyId">The ID of the company.</param>
        /// <param name="governorateId">The ID of the governorate.</param>
        /// <returns>A ResponseDto containing a list of StationReadDto.</returns>
        public async Task<ResponseDto> GetStationsByCompanyAndGovernorateAsync(int companyId, int governorateId)
        {
            try
            {
                var stations = await _context.Stations
                    .Include(s => s.City)
                    .Include(s => s.Governorate)
                    .OrderBy(s => s.Order) // Ensure stations are ordered by the 'Order' property
                    .Where(s => s.CompanyId == companyId && s.GovernorateId == governorateId)
                    .Select(s => new StationForSelectionDto

                    {
                        StationId = s.StationId,
                        CityName = s.City != null ? s.City.Name ?? string.Empty : string.Empty,
                        Address = s.Address ?? string.Empty,
                        DurationToEndStation = s.DurationToEndStation,
                        ExtraFee = s.ExtraFee

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
                    BankName = ba.Bank != null ? ba.Bank.BankName : "غير متوفر",
                    AccountNumber = ba.AccountNumber,
                    AccountHolderName = ba.AccountHolderName,
                    LogoUrl = !string.IsNullOrEmpty(ba.Bank.LogoUrl) ? _baseUrl + ba.Bank.LogoUrl : string.Empty
                })
                .ToListAsync();

            return ResponseDto.SuccessResponse($"تم استرجاع حسابات الشركة البنكية بنجاح. ({bankAccounts.Count})", bankAccounts);
        }
        #endregion

        #region Book Trip Logic 
        /// <summary>
        /// Handles the full e-ticket booking process using PassengerId directly.
        /// </summary>
        public async Task<ResponseDto> BookTripAsync(int passengerId, BookingRequestDto request)
        {
            // --- 1. PRE-TRANSACTION VALIDATIONS ---

            // Optimization: Find the passenger by PassengerId directly 
            // (since it was extracted from the token extension)
            var passenger = await _context.Passengers.FirstOrDefaultAsync(p => p.PassengerId == passengerId);
            if (passenger == null)
                return ResponseDto.FailureResponse("Passenger profile not found.");

            // Validate TripRoute (which covers trip, station, company, and governorate)
            var tripRoute = await _context.TripRoutes
                .Include(tr => tr.Trip) // Include Trip to avoid extra query later
                .FirstOrDefaultAsync(tr => tr.TripRouteId == request.TripRouteId);

            if (tripRoute == null)
                return ResponseDto.FailureResponse("عذراً، مسار الرحلة المختار غير متاح حالياً.");

            // Use the trip from the tripRoute
            var trip = tripRoute.Trip;

            if (trip == null)
                return ResponseDto.FailureResponse("The requested trip does not exist.");

            // Ensure the trip is still 'Scheduled'
            if (trip.Status != TripStatus.scheduled)
                return ResponseDto.FailureResponse("This trip is no longer available for booking.");

            // Check if passengers list is empty 
            if (request.PassengerDetails == null || request.PassengerDetails.Count == 0)
                return ResponseDto.FailureResponse("يجب إدخال تفاصيل راكب واحد على الأقل. يرجى التأكد من إرسال البيانات بالتنسيق الصحيح.");

            if (request.PassengerDetails.Count > 10)
                return ResponseDto.FailureResponse("لا يمكنك حجز أكثر من 10 مقاعد.");

            // Check seat availability
            if (trip.AvailableSeats < request.PassengerDetails.Count)
                return ResponseDto.FailureResponse($"Insufficient seats. Only {trip.AvailableSeats} seats remaining.");

            var bankAccount = await _context.BankAccounts.FirstOrDefaultAsync(ba => ba.BankAccountId == request.BankAccountId);

            if (bankAccount == null)
                return ResponseDto.FailureResponse("Invalid bank account selection.");

            // --- 2. CALCULATE PRICING ---
            decimal totalAmount = tripRoute.RouteFare * request.PassengerDetails.Count;

            // No image processing in this stage

            // --- 3. EXECUTION STRATEGY ---
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // A. Create the Main Booking Header
                    var booking = new Booking
                    {
                        PassengerId = passenger.PassengerId, // Linked to the fetched profile
                        TripRouteId = tripRoute.TripRouteId,
                        BankAccountId = request.BankAccountId,
                        NumberOfSeats = request.PassengerDetails.Count,
                        TotalAmount = totalAmount,
                        ReceiptImagePath = null, // Will be updated in stage 2
                        Status = BookingStatus.PendingAttachment,
                        BookingAt = DateHelper.GetYemenTime()
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    // B. Create Passenger Details and E-Tickets
                    foreach (var passengerDto in request.PassengerDetails)
                    {
                        var passengerDetail = new PassengerDetails
                        {
                            BookingId = booking.BookingId,
                            FullName = passengerDto.FullName,
                            NationalId = passengerDto.NationalId,
                            PhoneNumber = passengerDto.PhoneNumber,
                            BirthDate = passengerDto.BirthDate,
                            Gender = passengerDto.Gender
                        };

                        _context.PassengerDetails.Add(passengerDetail);
                        await _context.SaveChangesAsync();

                        // Generate a placeholder E-Ticket
                        _context.ETickets.Add(new ETicket
                        {
                            PassengerDetailId = passengerDetail.PassengerDetailsId,
                            TicketCode = null,
                            Status = ETicketStatus.Active,
                            IsConfirmed = false
                        });
                    }

                    // C. Inventory Management
                    trip.AvailableSeats -= request.PassengerDetails.Count;
                    if (trip.AvailableSeats == 0)
                        trip.Status = TripStatus.Fulled;

                    _context.Trips.Update(trip);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return ResponseDto.SuccessResponse("Stage 1 complete: Booking created. Please upload the receipt.", booking.BookingId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    return ResponseDto.FailureResponse($"System Error: {ex.Message}");
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
    }
}