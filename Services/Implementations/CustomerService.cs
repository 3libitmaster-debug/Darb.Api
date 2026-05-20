using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Booking;
using Darb.Api.DTOs.customer;
using Darb.Api.DTOs.customer.MyBookings;
using Darb.Api.DTOs.passengerDtos.bookingDtos;
using Darb.Api.DTOs.passengerDtos.homePageDtos;
using Darb.Api.DTOs.passengerDtos.settings;
using Darb.Api.Extensions;
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

  public class CustomerService : ICustomerService
  {
    private readonly IRepository<Governorate> _govRepo;
    private readonly IRepository<Advertisement> _adRepo;
    private readonly IRepository<Company> _companyRepo;
    private readonly ApplicationDbContext _context;
    private readonly IImageService _imageService;

    // Base URL for external links and images (injected via Options Pattern)
    private readonly string _baseUrl;

    public CustomerService(
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
    /// Searches for scheduled trips based on dynamic User filters.
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
        // Filters are only applied if the User provides a value (> 0).

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

    #region Company Bank Users Retrieval Logic
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
    /// Handles seat inventory, multi-customer registration, and owner-as-customer injection.
    /// </summary>
    public async Task<ResponseDto> BookTripAsync(int customerId, BookingRequestDto request)
    {
      // 1. PRE-TRANSACTION VALIDATIONS ---

      // Validate the existence of the TripRoute and the associated Trip
      var TripRoute = await _context.TripRoutes
          .Include(tr => tr.Trip)
          .FirstOrDefaultAsync(tr => tr.TripRouteId == request.TripRouteId);

      if (TripRoute == null || TripRoute.Trip == null)
        return ResponseDto.FailureResponse("مسار الرحلة المختار غير متاح حالياً.");

      var trip = TripRoute.Trip;

      // Ensure the trip is still open for booking
      if (trip.TripStatus != TripStatus.scheduled)
        return ResponseDto.FailureResponse("عذراً، هذه الرحلة لم تعد متاحة للحجز.");

      // Calculate total seats required from the provided customers list
      int totalSeatsRequired = request.AdditionalPassengers?.Count ?? 0;

      if (totalSeatsRequired == 0)
        return ResponseDto.FailureResponse("يجب إضافة راكب واحد على الأقل.");

      // Verify physical seat availability in the bus
      if (trip.AvailableSeats < totalSeatsRequired)
        return ResponseDto.FailureResponse($"عذراً، لا توجد مقاعد كافية. المقاعد المتاحة: {trip.AvailableSeats}");

      // Calculate total financial amount based on route fare
      decimal totalAmount = TripRoute.SeatFare * totalSeatsRequired;

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
            CustomerId = customerId,
            TripRouteId = TripRoute.TripRouteId,
            ReservedSeatsCount = totalSeatsRequired,
            TotalAmount = totalAmount,
            Status = BookingStatus.PendingAttachment, // Phase 1: Waiting for receipt upload
            BookingAt = DateHelper.GetYemenTime()
          };

          _context.Bookings.Add(booking);
          await _context.SaveChangesAsync(); // Commit to generate BookingId for FK relations

          // B. Build Unified Customer List from DTOs
          var allPassengersList = new List<Passenger>();

          // Map all customers from the request
          foreach (var pDto in request.AdditionalPassengers)
          {
            allPassengersList.Add(new Passenger
            {
              BookingId = booking.BookingId,
              FullName = pDto.FullName,
              NationalId = pDto.NationalId,
              PhoneNumber = pDto.PhoneNumber,
              BirthDate = pDto.BirthDate,
            });
          }

          // Bulk save all customers to optimize database performance
          _context.Passenger.AddRange(allPassengersList);
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
    public async Task<ResponseDto> UploadReceiptAsync(int customerId, UploadReceiptDto request)
    {
      var customer = await _context.Customers.FirstOrDefaultAsync(p => p.CustomerId == customerId);
      if (customer == null)
        return ResponseDto.FailureResponse("Customer profile not found.");

      var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == request.BookingId && b.CustomerId == customer.CustomerId);

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

    #region Customer Profile Logic
    public async Task<ResponseDto> GetProfileAsync(int customerId)
    {
      try
      {
        var profile = await _context.Customers
            .Include(p => p.User)
            .Where(p => p.CustomerId == customerId)
            .Select(p => new PassengerProfileDto
            {
              CustomerId = p.CustomerId,
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

    #region My Bookings Retrieval Logic


    public async Task<ResponseDto> GetBookingStatusesAsync()
    {
      try
      {
        var statuses = Enum.GetValues(typeof(BookingStatus))
            .Cast<BookingStatus>()
            .Select(s => new BookingStatusDto
            {
              Id = (int)s,
              StatusName = s.GetDisplayName()
            })
            .ToList();

        return ResponseDto.SuccessResponse(data: statuses, message: "Success");
      }
      catch (Exception ex)
      {
        return ResponseDto.FailureResponse($"Error: {ex.Message}");
      }
    }


    public async Task<ResponseDto> GetBookingsByStatusAsync(int customerId, BookingStatus status)
    {
      try
      {
        // 1. تحقق إضافي لضمان أن القيمة الممررة موجودة ضمن الـ Enum المعرف لدينا
        if (!Enum.IsDefined(typeof(BookingStatus), status))
        {
          return ResponseDto.FailureResponse("حالة الحجز غير معرفة في النظام.");
        }

        // 2. جلب البيانات مع الفلترة والتحويل إلى DTO
        var bookings = await _context.Bookings
            .Include(b => b.TripRoute)
                .ThenInclude(ts => ts!.Trip)
                    .ThenInclude(t => t!.Company)
            .Include(b => b.TripRoute)
                .ThenInclude(ts => ts!.Trip)
                    .ThenInclude(t => t!.StartGovernate)
            .Include(b => b.TripRoute)
                .ThenInclude(ts => ts!.Trip)
                    .ThenInclude(t => t!.EndGovernate)
            .Where(b => b.CustomerId == customerId && b.Status == status)
            .OrderByDescending(b => b.BookingAt)
            .Select(b => new BookingByStatusDto
            {
              BookingId = b.BookingId,
              TotalAmount = b.TotalAmount,
              // جلب اسم الشركة مع التحقق من النل
              CompanyName = b.TripRoute != null && b.TripRoute.Trip != null && b.TripRoute.Trip.Company != null
                              ? b.TripRoute.Trip.Company.Name ?? "غير متوفر" : "غير متوفر",

              // معالجة رابط الشعار باستخدام الـ BaseUrl
              CompanyLogo = b.TripRoute != null && b.TripRoute.Trip != null && b.TripRoute.Trip.Company != null && !string.IsNullOrEmpty(b.TripRoute.Trip.Company.Logo)
                              ? _baseUrl + b.TripRoute.Trip.Company.Logo : string.Empty,

              // جلب بيانات المحافظات
              StartGovernorate = b.TripRoute != null && b.TripRoute.Trip != null && b.TripRoute.Trip.StartGovernate != null
                                   ? b.TripRoute.Trip.StartGovernate.Name ?? "غير متوفر" : "غير متوفر",

              EndGovernorate = b.TripRoute != null && b.TripRoute.Trip != null && b.TripRoute.Trip.EndGovernate != null
                                 ? b.TripRoute.Trip.EndGovernate.Name ?? "غير متوفر" : "غير متوفر",
            })
            .ToListAsync();

        // 3. التحقق من وجود نتائج وإرسال الاستجابة المناسبة
        if (bookings == null || bookings.Count == 0)
        {
          return ResponseDto.SuccessResponse("لا توجد حجوزات متوفرة لهذه الحالة حالياً.", new List<BookingByStatusDto>());
        }

        return ResponseDto.SuccessResponse($"تم استرجاع الحجوزات بنجاح. ({bookings.Count})", bookings);
      }
      catch (Exception ex)
      {
        // تسجيل الخطأ أو إرجاع رسالة فشل
        return ResponseDto.FailureResponse($"فشل استرجاع الحجوزات: {ex.Message}");
      }
    }

    public async Task<ResponseDto> GetBookingDetailsAsync(int bookingId, int customerId)
    {
      try
      {
        // استخدام Include للوصول إلى كافة الجداول المرتبطة بناءً على الـ Model الجديد
        var booking = await _context.Bookings
            .Include(b => b.TripRoute)
                .ThenInclude(ts => ts!.Trip)
                    .ThenInclude(t => t!.Company)
            .Include(b => b.TripRoute)
                .ThenInclude(ts => ts!.Trip)
                    .ThenInclude(t => t!.StartGovernate)
            .Include(b => b.TripRoute)
                .ThenInclude(ts => ts!.Trip)
                    .ThenInclude(t => t!.EndGovernate)
            .Include(b => b.ETicket)
            .Include(b => b.Customers) // الربط مع كيان Passenger
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId);

        if (booking == null)
          return ResponseDto.FailureResponse("تفاصيل الحجز غير موجودة.");

        // بناء الـ DTO يدوياً لضمان الدقة في التحويل
        var details = new BookingDetailsDto
        {
          BookingId = booking.BookingId,
          BookingDate = booking.BookingAt.ToString("yyyy-MM-dd HH:mm"),
          ReservedSeatsCount = booking.ReservedSeatsCount,
          TotalAmount = booking.TotalAmount,
          StatusId = (int)booking.Status,
          StatusName = booking.Status.GetDisplayName(), // الميثود التي أنشأناها باللغة العربية

          // بيانات الرحلة
          CompanyName = booking.TripRoute?.Trip?.Company?.Name ?? "غير متوفر",
          CompanyLogo = !string.IsNullOrEmpty(booking.TripRoute?.Trip?.Company?.Logo)
                          ? _baseUrl + booking.TripRoute.Trip.Company.Logo : string.Empty,
          StartGovernorate = booking.TripRoute?.Trip?.StartGovernate?.Name ?? "غير متوفر",
          EndGovernorate = booking.TripRoute?.Trip?.EndGovernate?.Name ?? "غير متوفر",
          DepartureDate = booking.TripRoute?.Trip?.DepDate.ToString("yyyy-MM-dd") ?? string.Empty,
          DepartureTime = booking.TripRoute?.DepartureTime.ToString("hh:mm tt") ?? string.Empty,

          // بيانات التذكرة (في حال وجودها)
          TicketCode = booking.ETicket?.TicketCode ?? "بانتظار التأكيد",
          TicketStatus = booking.ETicket != null ? booking.ETicket.Status.ToString() : "N/A",

          // تحويل قائمة الركاب بناءً على كيان Passenger الخاص بك
          Customers = booking.Customers.Select(p => new PassengerItemDto
          {
            FullName = p.FullName,
            NationalId = p.NationalId,
            PhoneNumber = p.PhoneNumber
          }).ToList()
        };

        return ResponseDto.SuccessResponse(data: details, message: "تم جلب تفاصيل الحجز بنجاح.");
      }
      catch (Exception ex)
      {
        return ResponseDto.FailureResponse($"خطأ أثناء جلب تفاصيل الحجز: {ex.Message}");
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
    public async Task<ResponseDto> AddReviewAsync(int customerId, AddReviewDto request)
    {
      try
      {
        var company = await _context.Companies.FindAsync(request.CompanyId);
        if (company == null) return ResponseDto.FailureResponse("الشركة غير موجودة");

        var review = new Review
        {
          CustomerId = customerId,
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
    /// Retrieves all reviews for a customer using ReviewReturnDto.
    /// </summary>
    public async Task<ResponseDto> GetPassengerReviewsAsync(int customerId)
    {
      try
      {
        var reviews = await _context.Review
            .Where(r => r.CustomerId == customerId)
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
    public async Task<ResponseDto> UpdateReviewAsync(int customerId, int reviewId, UpdateReviewDto request)
    {
      try
      {
        var review = await _context.Review
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.CustomerId == customerId);

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
    public async Task<ResponseDto> DeleteReviewAsync(int customerId, int reviewId)
    {
      try
      {
        var review = await _context.Review
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.CustomerId == customerId);

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