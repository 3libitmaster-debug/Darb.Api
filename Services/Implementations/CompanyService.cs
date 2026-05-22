using Darb.Api.Dtos;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Booking;
using Darb.Api.DTOs.Trip;
using Darb.Api.DTOs.BankAccount;
using Darb.Api.DTOs.TripFare;
using Darb.Api.DTOs.TripRoute;
using Darb.Api.Extensions;
using Darb.Api.Helpers;
using Darb.Api.Models;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Darb.Api.Models.Enums;
using Darb.Api.DTOs.company;
using Darb.Api.Enums;
using Darb.Api.DTOs.company.TripRoute;
using Darb.Api.DTOs.company.Booking;

namespace Darb.Api.Services.Implementations
{
    public class CompanyService : ICompanyService
    {
        private readonly IRepository<Trip> _tripRepository;
        private readonly IRepository<Bus> _busRepository;
        private readonly IRepository<Station> _stationRepository;
        private readonly ApplicationDbContext _context;
        private readonly IQrCodeService _qrCodeService;
        private readonly IImageService _imageService;
        private readonly INotificationService _notificationService; // 1. إضافة الحقل الخاص بالخدمة
        private readonly string _baseUrl;

        public CompanyService(
            IRepository<Trip> tripRepository,
            IRepository<Bus> busRepository,
            IRepository<Station> stationRepository,
            ApplicationDbContext context,
            IQrCodeService qrCodeService,
            IImageService imageService,
            INotificationService notificationService, // 2. تمرير الخدمة في المشيّد
            IOptions<ApiSettings> apiOptions)
        {
            _tripRepository = tripRepository;
            _busRepository = busRepository;
            _stationRepository = stationRepository;
            _context = context;
            _qrCodeService = qrCodeService;
            _imageService = imageService;
            _notificationService = notificationService; // 3. إسناد القيمة للحقل
            _baseUrl = apiOptions.Value.BaseUrl ?? string.Empty;
        }

    



        #region Trip Management Logic

        
        /// <summary>
        /// Retrieves all trips owned by the authenticated company with related data.
        /// </summary>
        public async Task<ResponseDto> GetAllCompanyTripsAsync(int companyId)
        {
            // STEP 1: Data Retrieval
            var trips = await _context.Trips
                .Include(t => t.Bus)
                .Include(t => t.StartGovernate)
                .Include(t => t.EndGovernate)
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();

            bool hasChanges = false;

            // STEP 2: Use Yemen Local Time for comparison
            DateTime currentYemenTime = DateHelper.GetYemenTime();

            // STEP 3: Real-time Status Synchronization Logic
            foreach (var trip in trips)
            {
                if (trip.TripStatus == TripStatus.cancelled)
                {
                    continue;
                }

                // Check if trip time has passed
                if (trip.TripStatus != TripStatus.completed && trip.DepDate <= currentYemenTime)
                {
                    trip.TripStatus = TripStatus.completed;
                    hasChanges = true;
                    continue;
                }

                // Capacity-based updates
                if (trip.TripStatus == TripStatus.scheduled && trip.AvailableSeats <= 0)
                {
                    trip.TripStatus = TripStatus.Fulled;
                    hasChanges = true;
                }
                else if (trip.TripStatus == TripStatus.Fulled && trip.AvailableSeats > 0)
                {
                    trip.TripStatus = TripStatus.scheduled;
                    hasChanges = true;
                }
            }

            // STEP 4: Persist changes if any statuses were updated during the loop.
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            // STEP 5: Map to DTOs for the final response (With Arabic Status Names)
            var tripList = trips
                .OrderByDescending(t => t.DepDate)
                .Select(t => new TripReadDto
                {
                    TripId = t.TripId,
                    StartGoveName = t.StartGovernate?.Name ?? "N/A",
                    EndGoveName = t.EndGovernate?.Name ?? "N/A",
                    Price = t.Price,
                    DepartureDate = t.DepDate,
                    Status = t.TripStatus.GetDisplayName(),
                    AvailableSeats = t.AvailableSeats,
                    BusId = t.BusId
                }).ToList();

            return ResponseDto.SuccessResponse($"تم العثور على ({tripList.Count}) رحلة بنجاح.", tripList);
        }
        

        
        /// <summary>
        /// Retrieves specific trip details after ensuring the company owns the trip.
        /// </summary>
        public async Task<ResponseDto> GetTripByIdAsync(int tripId, int companyId)
        {
            // STEP 1: Secure Data Retrieval
            // Fetch the specific trip ensuring it belongs to the authenticated company.
            var trip = await _context.Trips
                .Include(t => t.Bus)
                .Include(t => t.StartGovernate)
                .Include(t => t.EndGovernate)
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);

            if (trip == null)
                return ResponseDto.FailureResponse("لم يتم العثور على الرحلة.");

            // Real-time Status Synchronization Logic
            bool statusUpdated = false;

            // Check for 'Full' status if still scheduled.
            if (trip.TripStatus == TripStatus.scheduled && trip.AvailableSeats <= 0)
            {
                trip.TripStatus = TripStatus.Fulled;
                statusUpdated = true;
            }

            // STEP 3: Save only if the status changed
            if (statusUpdated)
            {
                await _context.SaveChangesAsync();
            }

            // STEP 4: Mapping to TripReadDto
            var tripDto = new TripReadDto
            {
                TripId = trip.TripId,
                StartGoveName = trip.StartGovernate?.Name ?? "N/A",
                EndGoveName = trip.EndGovernate?.Name ?? "N/A",
                Price = trip.Price,
                Status = trip.TripStatus.ToString(),
                AvailableSeats = trip.AvailableSeats,
                DepartureDate = trip.DepDate,
                BusId = trip.BusId
            };

            return ResponseDto.SuccessResponse("تم ارجاع بيانات الرحلة", tripDto);
        }
        

        
        /// <summary>
        /// Validates bus ownership and availability before creating a new scheduled trip.
        /// </summary>
        public async Task<ResponseDto> createTripAsync(CreateTripDto tripDto, int companyId)
        {
            // 1. التحقق من أن البيانات لم تصل فارغة
            if (tripDto == null || tripDto.StartGoveId == 0)
            {
                return ResponseDto.FailureResponse("عذراً، لم يتم استلام بيانات الرحلة بشكل صحيح.");
            }

            var nowYemen = DateHelper.GetYemenTime();

            // 2. التحقق من منطقية التاريخ
            if (tripDto.DepartureDate.Date < nowYemen.Date)
            {
                return ResponseDto.FailureResponse($"عذراً، تاريخ الرحلة ({tripDto.DepartureDate:yyyy-MM-dd}) لا يمكن أن يكون في الماضي.");
            }

            // 3. التحقق من الحافلة وملكية الشركة لها
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == tripDto.BusId && b.CompanyId == companyId);

            if (bus == null)
                return ResponseDto.FailureResponse("الحافلة المختارة غير موجودة أو غير مسجلة لشركتكم.");

            // 4. جلب التسعيرات والتحقق من المسار للحصول على السعر الأساسي
            var tripFares = await _context.TripFares
                .Where(tf => tf.CompanyId == companyId &&
                             tf.FromGovId == tripDto.StartGoveId &&
                             tf.ToGovId == tripDto.EndGoveId)
                .ToListAsync();

            if (!tripFares.Any())
                return ResponseDto.FailureResponse("لا توجد تسعيرات معرفة لهذا المسار في النظام.");

            var primaryFare = tripFares.FirstOrDefault(tf => tf.IsMainStation);
            if (primaryFare == null)
                return ResponseDto.FailureResponse("يجب تحديد السعر الرئيسي للمسار (Main Station) في الإعدادات.");

            // 5. إنشاء الكائن الرئيسي للرحلة
            var trip = new Trip
            {
                BusId = tripDto.BusId,
                CompanyId = companyId,
                StartGoveId = tripDto.StartGoveId,
                EndGoveId = tripDto.EndGoveId,
                DepDate = tripDto.DepartureDate,
                Price = primaryFare.Price,
                TripStatus = TripStatus.scheduled,
                AvailableSeats = bus.BusCapacity,
                Period = tripDto.Period
            };

            await _context.Trips.AddAsync(trip);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم إنشاء الرحلة بنجاح. يمكنك الآن إضافة المسارات.", new { tripId = trip.TripId });
        }

       
       

        
        /// <summary>
        /// Performs partial updates on a trip while protecting historical completed data.
        /// </summary>
        public async Task<ResponseDto> UpdateTripAsync(int tripId, UpdateTripDto updateDto, int companyId)
        {
            // Security: Fetch the trip ensuring it belongs to the current company context.
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);

            if (trip == null)
                return ResponseDto.FailureResponse("نأسف، الرحلة غير موجودة أو لا تملك الصلاحية اللازمة لتعديلها.");

            // Logic: Prevent modification if the trip is completed.
            if (trip.TripStatus == TripStatus.completed)
            {
                return ResponseDto.FailureResponse("لا يمكن تعديل بيانات هذه الرحلة نظراً لكونها مكتملة ومؤرشفة في السجلات المالية.");
            }

            // Partial Mapping: Update fields only if new values are provided in the DTO.

            if (updateDto.DepartureDate.HasValue)
            {
                if (updateDto.DepartureDate.Value.Date < DateTime.Now.Date)
                    return ResponseDto.FailureResponse("يرجى اختيار تاريخ مستقبلي؛ لا يمكن تعديل وقت الانطلاق لوقت قد مضى.");
                trip.DepDate = updateDto.DepartureDate.Value;
            }

            // Bus Swap Validation: Ensure the new bus is also owned by this company.
            if (updateDto.BusId.HasValue && updateDto.BusId.Value != trip.BusId)
            {
                var busExists = await _context.Buses
                    .AnyAsync(b => b.BusId == updateDto.BusId.Value && b.CompanyId == companyId);

                if (!busExists)
                    return ResponseDto.FailureResponse("الحافلة الجديدة المختارة غير تابعة لشركتكم، يرجى مراجعة بيانات الأسطول.");

                trip.BusId = updateDto.BusId.Value;
            }

            try
            {
                await _context.SaveChangesAsync();

                var resultDto = new TripDto { TripId = trip.TripId, BasePrice = trip.Price, Status = trip.TripStatus.ToString() };
                return ResponseDto.SuccessResponse("تم تحديث بيانات الرحلة والمسارات التابعة لها بنجاح وفق التعديلات الجديدة.", resultDto);
            }
            catch (Exception ex)
            {
                var realError = ex.InnerException?.Message ?? ex.Message;
                return ResponseDto.FailureResponse($"حدث خطأ أثناء محاولة تحديث البيانات في قاعدة البيانات: {realError}");
            }
        }
       

        
        /// <summary>
        /// Permanently removes a trip from the database after verifying status.
        /// </summary>
        public async Task<ResponseDto> DeleteTripAsync(int tripId, int companyId)
        {
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);

            if (trip == null)
                return ResponseDto.FailureResponse("نعتذر، لم يتم العثور على الرحلة المراد حذفها.");

            // Safety Rule: Completed trips should remain in history and cannot be deleted.
            if (trip.TripStatus == TripStatus.completed)
                return ResponseDto.FailureResponse("حفاظاً على سلامة السجلات المالية والإحصائية، لا يمكن حذف الرحلات المكتملة.");

            // --- NEW: Booking Check ---
            // Check if there are any bookings associated with this trip's routes
            var hasBookings = await _context.Bookings
                .AnyAsync(b => _context.TripRoutes.Where(tr => tr.TripId == tripId).Select(tr => tr.TripRouteId).Contains(b.TripRouteId));

            if (hasBookings)
                return ResponseDto.FailureResponse("لا يمكن حذف هذه الرحلة لوجود حجزاًت فعالة مرتبطة بمساراتها.");
            

            var relatedSchedules = await _context.TripRoutes.Where(tr => tr.TripId == tripId).ToListAsync();
            _context.TripRoutes.RemoveRange(relatedSchedules);

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم حذف سجل الرحلة من النظام بنجاح.");
        }
        #endregion

        #region Trip Routes Management

        public async Task<ResponseDto> GetAllTripRoutesAsync(int tripId, int companyId)
        {
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);

            if (trip == null) return ResponseDto.FailureResponse("الرحلة غير موجودة أو لا تملك صلاحية الوصول إليها.");

            var schedules = await _context.TripRoutes
                .Include(ts => ts.Station)
                    .ThenInclude(s => s!.City)
                .Where(ts => ts.TripId == tripId)
                .Select(ts => new TripRouteReadDto
                {
                    TripRouteId = ts.TripRouteId,
                    TripId = ts.TripId,
                    StationId = ts.StationId,
                    StationName = ts.Station != null ? ts.Station.Address : "غير متوفر",
                    CityName = ts.Station != null && ts.Station.City != null ? ts.Station.City.Name : "غير متوفر",
                    DepartureTime = ts.DepartureTime,
                    SeatFare = ts.SeatFare
                })
                .ToListAsync();

            return ResponseDto.SuccessResponse($"تم استرجاع ({schedules.Count}) محطات توقف للرحلة بنجاح.", schedules);
        }

        public async Task<ResponseDto> GetTripRouteByIdAsync(int scheduleId, int companyId)
        {
            var ts = await _context.TripRoutes
                .Include(ts => ts.Trip)
                .Include(ts => ts.Station)
                    .ThenInclude(s => s!.City)
                .FirstOrDefaultAsync(t => t.TripRouteId == scheduleId && t.Trip!.CompanyId == companyId);

            if (ts == null) return ResponseDto.FailureResponse("محطة التوقف غير موجودة أو لا تملك صلاحية الوصول إليها.");

            var dto = new TripRouteReadDto
            {
                TripRouteId = ts.TripRouteId,
                TripId = ts.TripId,
                StationId = ts.StationId,
                StationName = ts.Station?.Address ?? "غير متوفر",
                CityName = ts.Station?.City?.Name ?? "غير متوفر",
                DepartureTime = ts.DepartureTime,
                SeatFare = ts.SeatFare
            };

            return ResponseDto.SuccessResponse("تم استرجاع بيانات محطة التوقف بنجاح.", dto);
        }

        public async Task<ResponseDto> AddTripRouteAsync(int tripId, AddTripRouteDto route, int companyId)
        {
            // 1. التحقق من وجود الرحلة وملكية الشركة لها
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);

            if (trip == null)
                return ResponseDto.FailureResponse("الرحلة غير موجودة أو لا تملك صلاحية الوصول إليها.");

            if (route == null)
                return ResponseDto.FailureResponse("يجب تحديد بيانات التوجيه والمحطة ووقت الانطلاق.");

            // 2. جلب التسعيرات المتاحة لهذا المسار
            var matchingFare = await _context.TripFares
                .FirstOrDefaultAsync(tf => tf.CompanyId == companyId &&
                                         tf.FromGovId == trip.StartGoveId &&
                                         tf.ToGovId == trip.EndGoveId &&
                                         tf.StationId == route.StationId);

            if (matchingFare == null)
                return ResponseDto.FailureResponse($"المحطة رقم {route.StationId} غير مرتبطة بهذا المسار.");

            var tripRoute = new TripRoute
            {
                TripId = tripId,
                StationId = route.StationId,
                DepartureTime = route.DepartureTime ?? TimeOnly.MinValue,
                SeatFare = matchingFare.Price
            };

            await _context.TripRoutes.AddAsync(tripRoute);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم إضافة محطة التوقف للرحلة بنجاح.");
        }

        public async Task<ResponseDto> UpdateTripRouteAsync(int scheduleId, UpdateTripRouteDto dto, int companyId)
        {
            var ts = await _context.TripRoutes
                .Include(ts => ts.Trip)
                .FirstOrDefaultAsync(t => t.TripRouteId == scheduleId && t.Trip!.CompanyId == companyId);

            if (ts == null) return ResponseDto.FailureResponse("محطة التوقف غير موجودة.");

            try
            {
                if (dto.DepartureTime.HasValue)
                    ts.DepartureTime = dto.DepartureTime.Value;

                await _context.SaveChangesAsync();
                return ResponseDto.SuccessResponse("تم تحديث بيانات محطة التوقف بنجاح.");
            }
            catch (FormatException ex)
            {
                return ResponseDto.FailureResponse(ex.Message);
            }
        }

        public async Task<ResponseDto> DeleteTripRouteAsync(int scheduleId, int companyId)
        {
            var ts = await _context.TripRoutes
                .Include(ts => ts.Trip)
                .FirstOrDefaultAsync(t => t.TripRouteId == scheduleId && t.Trip!.CompanyId == companyId);

            if (ts == null) return ResponseDto.FailureResponse("محطة التوقف غير موجودة.");

            // Check if there are bookings for this schedule before deletion
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.TripRouteId == scheduleId);
            if (hasBookings)
                return ResponseDto.FailureResponse("لا يمكن حذف هذه المحطة لوجود حجوزات مؤكدة مرتبطة بها.");

            _context.TripRoutes.Remove(ts);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف محطة التوقف من الرحلة بنجاح.");
        }

        #endregion

        #region Bus Management Logic

        /// <summary>
        /// Retrieves the entire fleet of buses belonging to the company and maps them to Read DTOs.
        /// </summary>
        public async Task<ResponseDto> GetAllCompanyBusesAsync(int companyId)
        {
            // Fetching buses directly from the context for custom filtering and mapping
            var buses = await _context.Buses
                .Where(b => b.CompanyId == companyId)
                .ToListAsync();

            // Mapping to ReadDto to provide a clean API response
            var busList = buses.Select(b => new BusReadDto
            {
                BusId = b.BusId,
                PlateNumber = b.PlateNumber ?? "",
                Model = b.Model ?? "",
                Capacity = b.BusCapacity,
                Status = b.BusStatus.ToString() // Converts Enum to String for the client
            }).ToList();

            return ResponseDto.SuccessResponse($"تم استعادة بيانات الأسطول بنجاح، إجمالي الحافلات: {busList.Count}", busList);
        }

        /// <summary>
        /// Retrieves details of a specific bus, ensuring it belongs to the authenticated company.
        /// </summary>
        public async Task<ResponseDto> GetBusByIdAsync(int busId, int companyId)
        {
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == busId && b.CompanyId == companyId);

            if (bus == null)
                return ResponseDto.FailureResponse("نعتذر، لم يتم العثور على الحافلة المطلوبة، أو قد لا تملك صلاحية الوصول إليها.");

            var busDto = new BusReadDto
            {
                BusId = bus.BusId,
                PlateNumber = bus.PlateNumber ?? "غير محدد",
                Model = bus.Model ?? "غير محدد",
                Capacity = bus.BusCapacity,
                Status = bus.BusStatus.ToString()
            };

            return ResponseDto.SuccessResponse("تم استرجاع بيانات الحافلة بنجاح.", busDto);
        }

        /// <summary>
        /// Registers a new bus into the company's fleet using the Generic Repository.
        /// </summary>
        public async Task<ResponseDto> CreateBusAsync(CreateBusDto busDto, int companyId)
        {
            // Business Rule: Ensure plate numbers are unique across the system
            var exists = await _context.Buses.AnyAsync(b => b.PlateNumber == busDto.PlateNumber);
            if (exists)
                return ResponseDto.FailureResponse("عذراً، رقم اللوحة هذا مسجل مسبقاً في النظام، يرجى التأكد من صحة البيانات.");

            var bus = new Bus
            {
                PlateNumber = busDto.PlateNumber,
                Model = busDto.Model,
                BusCapacity = busDto.Capacity,
                BusStatus = BusStatus.Available, // New buses are available by default
                CompanyId = companyId
            };

            // Using the Generic Repository for adding the entity
            await _busRepository.AddAsync(bus);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم تسجيل الحافلة الجديدة بنجاح وانضمامها لأسطول شركتكم.");
        }

        /// <summary>
        /// Updates bus details or operational status while ensuring strict company ownership.
        /// </summary>
        public async Task<ResponseDto> UpdateBusAsync(int busId, UpdateBusDto busDto, int companyId)
        {
            // Security: Fetch bus ensuring it belongs to the current company context
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == busId && b.CompanyId == companyId);

            if (bus == null)
                return ResponseDto.FailureResponse("نعتذر، لم يتم العثور على الحافلة المطلوبة أو قد لا تملك صلاحية الوصول إليها.");

            // Partial Update Logic: Apply changes only if values are provided
            if (!string.IsNullOrEmpty(busDto.Model))
                bus.Model = busDto.Model;

            if (busDto.Capacity.HasValue && busDto.Capacity.Value > 0)
                bus.BusCapacity = busDto.Capacity.Value;

            if (busDto.Status.HasValue)
                bus.BusStatus = busDto.Status.Value;

            // Persisting changes via Generic Repository update pattern
            _busRepository.Update(bus);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم تحديث بيانات الحافلة في النظام بنجاح.");
        }

        /// <summary>
        /// Deletes a bus record only if it's not linked to any previous or upcoming trips.
        /// </summary>
        public async Task<ResponseDto> DeleteBusAsync(int busId, int companyId)
        {
            // Include trips to check for dependencies before deletion
            var bus = await _context.Buses
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.BusId == busId && b.CompanyId == companyId);

            if (bus == null)
                return ResponseDto.FailureResponse("عذراً، الحافلة المراد حذفها غير موجودة في سجلاتكم.");

            // Critical Rule: Prevent deletion if there is a trip history (Referential Integrity)
            if (bus.Trip != null && bus.Trip.Any())
            {
                return ResponseDto.FailureResponse("حفاظاً على سلامة السجلات التاريخية للرحلات، لا يمكن حذف حافلة مرتبطة بعمليات سابقة، نوصي بتغيير حالتها إلى 'خارج الخدمة' بدلاً من الحذف.");
            }

            // Perform permanent deletion via Generic Repository
            _busRepository.Delete(bus);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم حذف سجل الحافلة من النظام بشكل نهائي.");
        }

        /// <summary>
        /// Toggles the operational status of a bus between Available and UnderMaintenance while enforcing strict ownership.
        /// </summary>
        public async Task<ResponseDto> ToggleBusMaintenanceStatusAsync(int busId, int companyId)
        {
            // Security Context Check: Fetch the bus ensuring it belongs entirely to the authenticated company
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == busId && b.CompanyId == companyId);

            if (bus == null)
                return ResponseDto.FailureResponse("نعتذر، لم يتم العثور على الحافلة المطلوبة، أو قد لا تملك صلاحية الوصول إليها.");

            // State Machine Toggle: Switch states dynamically between Available and UnderMaintenance
            if (bus.BusStatus == BusStatus.Available)
            {
                bus.BusStatus = BusStatus.UnderMaintenance;
            }
            else if (bus.BusStatus == BusStatus.UnderMaintenance)
            {
                bus.BusStatus = BusStatus.Available;
            }
            else
            {
                // Business Rule Guard: Prevent automated toggling if the bus is in a critical state (e.g., OutOfService)
                return ResponseDto.FailureResponse($"عذراً، لا يمكن تغيير حالة الحافلة تلقائياً نظراً لأن حالتها الحالية هي: {bus.BusStatus}");
            }

            // Persisting the state change via Generic Repository update lifecycle pattern
            _busRepository.Update(bus);
            await _context.SaveChangesAsync();

            // Constructing localized dynamic success response message based on the new status
            string statusArabic = bus.BusStatus == BusStatus.Available ? "جاهزة للخدمة ومتاحة" : "في الصيانة حالياً";
            return ResponseDto.SuccessResponse($"تم تحديث حالة الحافلة التشغيلية بنجاح، الحالة الجديدة الآن: {statusArabic}");
        }

        #endregion

        #region Station Management Logic

        public async Task<ResponseDto> GetAllCompanyStationsAsync(int companyId)
        {
            var stations = await _context.Stations
                .Include(s => s.City)
                .Include(s => s.Governorate)
                .Where(s => s.CompanyId == companyId)
                .ToListAsync();

            var stationList = stations.Select(s => new Darb.Api.DTOs.Station.StationReadDto
            {
                StationId = s.StationId,
                Address = s.Address ?? "غير محدد",
                CityId = s.CityId,
                CityName = s.City?.Name ?? "غير محدد",
                GovernorateId = s.GovernorateId,
                GovernorateName = s.Governorate?.Name ?? "غير محدد",
                CompanyId = s.CompanyId
            }).ToList();

            return ResponseDto.SuccessResponse($"تم استرجاع المحطات بنجاح، إجمالي المحطات: {stationList.Count}", stationList);
        }

        public async Task<ResponseDto> GetStationByIdAsync(int stationId, int companyId)
        {
            var station = await _context.Stations
                .Include(s => s.City)
                .Include(s => s.Governorate)
                .FirstOrDefaultAsync(s => s.StationId == stationId && s.CompanyId == companyId);

            if (station == null)
                return ResponseDto.FailureResponse("نعتذر، لم يتم العثور على المحطة المطلوبة، أو قد لا تملك صلاحية الوصول إليها.");

            var stationDto = new Darb.Api.DTOs.Station.StationReadDto
            {
                StationId = station.StationId,
                Address = station.Address ?? "غير محدد",
                CityId = station.CityId,
                CityName = station.City?.Name ?? "غير محدد",
                GovernorateId = station.GovernorateId,
                GovernorateName = station.Governorate?.Name ?? "غير محدد",
                CompanyId = station.CompanyId
            };

            return ResponseDto.SuccessResponse("تم استرجاع بيانات المحطة بنجاح.", stationDto);
        }

        public async Task<ResponseDto> CreateStationAsync(Darb.Api.DTOs.Station.CreateStationDto stationDto, int companyId)
        {
            var station = new Darb.Api.Models.Station
            {
                Address = stationDto.Address,
                CityId = stationDto.CityId,
                GovernorateId = stationDto.GovernorateId,
                CompanyId = companyId
            };

            await _stationRepository.AddAsync(station);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم إضافة المحطة الجديدة بنجاح وانضمامها لشبكة المحطات.");
        }

        public async Task<ResponseDto> UpdateStationAsync(int stationId, Darb.Api.DTOs.Station.UpdateStationDto stationDto, int companyId)
        {
            var station = await _context.Stations
                .FirstOrDefaultAsync(s => s.StationId == stationId && s.CompanyId == companyId);

            if (station == null)
                return ResponseDto.FailureResponse("نعتذر، لم يتم العثور على المحطة المطلوبة أو قد لا تملك صلاحية الوصول إليها.");

            if (!string.IsNullOrEmpty(stationDto.Address))
                station.Address = stationDto.Address;

            if (stationDto.CityId.HasValue)
                station.CityId = stationDto.CityId.Value;

            if (stationDto.GovernorateId.HasValue)
                station.GovernorateId = stationDto.GovernorateId.Value;

            _stationRepository.Update(station);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم تحديث بيانات المحطة بنجاح.");
        }

     
       public async Task<ResponseDto> DeleteStationAsync(int stationId, int companyId)
        {
            var station = await _context.Stations
                .FirstOrDefaultAsync(s => s.StationId == stationId && s.CompanyId == companyId);

            if (station == null)
                return ResponseDto.FailureResponse("عذراً، المحطة المراد حذفها غير موجودة.");

            // Check if the station is linked to any active trips (To prevent foreign key constraint error)
            bool isLinkedToTrips = await _context.TripFares
                .AnyAsync(t => t.StationId == stationId );

            if (isLinkedToTrips)
                return ResponseDto.FailureResponse("لا يمكن حذف هذه المحطة لارتباطها برحلات مسجلة في النظام.");

            // استخدام الـ Context مباشرة للحذف لتوحيد السياق مع سطر الجلب العلوي
            _context.Stations.Remove(station);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم حذف بيانات المحطة بنجاح.");
        }

        #endregion

        #region Booking Management Logic

       

       

        
        
        public async Task<ResponseDto> ConfirmCompanyBookingClickAsync(int bookingId, int companyId)
        {
            // 1. Retrieve the booking with chained Includes to fetch Customers AND their underlying User accounts
            var booking = await _context.Bookings
                .Include(b => b.Customers)
                    .ThenInclude(c => c.User) // IMPORTANT: Chained Include to load the User entity containing the true UserId
                .Include(b => b.TripRoute)
                    .ThenInclude(tr => tr!.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId &&
                                          b.TripRoute != null &&
                                          b.TripRoute.Trip != null &&
                                          b.TripRoute.Trip.CompanyId == companyId);

            // 2. Validate booking existence and authorization
            if (booking == null)
                return ResponseDto.FailureResponse("عذراً، لم يتم العثور على الحجز، أو لا تملك الصلاحية لتأكيده.");

            // 3. Prevent re-confirming an already confirmed booking
            if (booking.Status == BookingStatus.Confirmed)
                return ResponseDto.FailureResponse("هذا الحجز تم تأكيده مسبقاً.");

            // 4. Update booking status to Confirmed
            booking.Status = BookingStatus.Confirmed;

            // 5. Check if an E-Ticket already exists for this booking to handle or reuse it
            var existingTicket = await _context.ETickets.FirstOrDefaultAsync(e => e.BookingId == booking.BookingId);
            int eticketId = existingTicket?.Id ?? 0;
            int tripRouteId = booking.TripRouteId;

            // 6. Generate a secure, Base64-encoded QR Code payload with the current booking metadata
            string payload = $"TripRouteId:{tripRouteId}|BookingId:{booking.BookingId}|ETicketId:{eticketId}";
            string qrBase64 = _qrCodeService.GenerateQrCodeBase64(payload);

            // 7. Insert a new ticket or update the existing ticket details accordingly
            if (existingTicket == null)
            {
                var ticket = new ETicket
                {
                    BookingId = booking.BookingId,
                    TicketCode = qrBase64,
                    Status = ETicketStatus.Valid
                };
                await _context.ETickets.AddAsync(ticket);
            }
            else
            {
                existingTicket.TicketCode = qrBase64;
                existingTicket.Status = ETicketStatus.Valid;
            }

            // 8. Commit structural database changes before proceeding to dispatch external notifications
            await _context.SaveChangesAsync();

            #region Automated Customer Notification Dispatch

            try
            {
                // Enforce safe access by checking both Customers and their navigated User entity
                if (booking.Customers != null && booking.Customers.User != null)
                {
                    // Now safely accessing the loaded UserId from the relational chain
                    int receiverUserId = booking.Customers.User.UserId;

                    string notificationTitle = "تم تأكيد حجزك بنجاح! 🎉";
                    string notificationBody = $"عزيزي المسافر، تم تأكيد حجزك للرحلة رقم {booking.TripRouteId}. يمكنك الآن استعراض تذكرتك الإلكترونية داخل التطبيق.";

                    // Dispatch notification asynchronously via the persistent Firebase FCM service layer
                    await _notificationService.SendIndividualNotificationAsync(
                        receiverId: receiverUserId,
                        title: notificationTitle,
                        body: notificationBody,
                        category: NotificationCategory.Transaction,
                        senderType: SenderRole.Company,
                        senderCompanyId: companyId
                    );
                }
            }
            catch (Exception ex)
            {
                // Enforce fault tolerance: failures in Firebase FCM delivery should not abort the successful database state
                // _logger.LogError(ex, "Automated booking confirmation push notification delivery failed.");
            }

            #endregion

            return ResponseDto.SuccessResponse("تم تأكيد الحجز بنجاح!");
        }

        public async Task<ResponseDto> GetTripBookingsAsync(int tripId, int companyId)
        {
            // 1. Verify that the trip exists and belongs to the company
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);
            if (trip == null)
            {
                return ResponseDto.FailureResponse("الرحلة غير موجودة أو لا تملك صلاحية الوصول إليها.");
            }

            // 2. Fetch only confirmed bookings for this trip
            var bookings = await _context.Bookings
                .Include(b => b.Customers)
                .Include(b => b.TripRoute)
                    .ThenInclude(tr => tr!.Station)
                        .ThenInclude(s => s!.City) // <-- السطر المضاف لربط جدول المدن
                .Where(b => b.TripRoute != null && b.TripRoute.TripId == tripId && b.Status == BookingStatus.Confirmed)
                .OrderByDescending(b => b.BookingAt)
                .ToListAsync();

            // 3. Map to DTO
            var bookingList = bookings.Select(b => new TripBookingReadDto
            {
                CustomerId = b.CustomerId,
                CustomerName = b.Customers?.FullName ?? "غير محدد",
                ReservedSeatsCount = b.ReservedSeatsCount,
                TotalAmount = b.TotalAmount,
                StationName = b.TripRoute?.Station?.City?.Name ?? "غير محدد", // تم إضافة الـ ? بعد City للحماية
                BookingAt = b.BookingAt,
                ReceiptImagePath = !string.IsNullOrEmpty(b.ReceiptImagePath) ? _baseUrl + b.ReceiptImagePath : null
            }).ToList();

            return ResponseDto.SuccessResponse($"تم استرجاع ({bookingList.Count}) حجز مؤكد للرحلة بنجاح.", bookingList);
        }

        public async Task<ResponseDto> RejectCompanyBookingAsync(int bookingId, int companyId)
        {
            // 1. Retrieve the booking along with its passenger collection and deep relational trip data
            var booking = await _context.Bookings
                .Include(b => b.Customers) // Collection of Passengers
                    .ThenInclude(p => p.User) // Chained include for the user account (to get UserId)
                .Include(b => b.TripRoute)
                    .ThenInclude(tr => tr!.Trip) // Accessing the core Trip entity
                .FirstOrDefaultAsync(b => b.BookingId == bookingId &&
                                          b.TripRoute != null &&
                                          b.TripRoute.Trip != null &&
                                          b.TripRoute.Trip.CompanyId == companyId);

            // 2. Validate booking existence and company ownership authorization
            if (booking == null)
                return ResponseDto.FailureResponse("عذراً، لم يتم العثور على الحجز أو لا توجد صلاحية.");

            // 3. Check if the booking is already rejected to avoid redundant processing
            if (booking.Status == BookingStatus.Rejected)
                return ResponseDto.FailureResponse("هذا الحجز مرفوض بالفعل.");

            // 4. Update booking status to Rejected
            booking.Status = BookingStatus.Rejected;

            // 5. Restore the reserved seats back to the core Trip capacity (Matching Darb Background Service pattern)
            if (booking.TripRoute?.Trip != null)
            {
                // Restoring the specific count of reserved seats back to the Trip's available seats
                booking.TripRoute.Trip.AvailableSeats += booking.ReservedSeatsCount;
            }

            // 6. Invalidate any associated E-Ticket for security and validation integrity
            var ticket = await _context.ETickets.FirstOrDefaultAsync(e => e.BookingId == bookingId);
            if (ticket != null)
            {
                ticket.Status = ETicketStatus.UnValid;
            }

            // 7. Commit state changes to the database before launching external communication threads
            await _context.SaveChangesAsync();

            #region Automated Customer Cancellation Notification Dispatch

            try
            {
                if (booking.Customers != null && booking.Customers.User != null)
                {
                    // Now safely accessing the loaded UserId from the relational chain
                    int receiverUserId = booking.Customers.User.UserId;

                    string notificationTitle = "تنبيه: تم رفض حجزك ⚠️";
                    string notificationBody = $"نعتذر منك، لقد تم رفض حجزك للرحلة رقم {booking.TripRouteId} من قبل شركة النقل. للمزيد من التفاصيل يرجى مراجعة التطبيق.";

                    // Dispatch notification asynchronously via the persistent Firebase FCM service layer
                    await _notificationService.SendIndividualNotificationAsync(
                        receiverId: receiverUserId,
                        title: notificationTitle,
                        body: notificationBody,
                        category: NotificationCategory.Transaction,
                        senderType: SenderRole.Company,
                        senderCompanyId: companyId
                    );
                }
            }
            catch (Exception ex)
            {
                // Maintain fault isolation: do not revert the database status if Firebase messaging encounters an issue
                // _logger.LogError(ex, "Automated booking rejection push notification delivery failed.");
            }

            #endregion

            return ResponseDto.SuccessResponse("تم رفض الحجز وتحديث حالته بنجاح.");
        }

        public async Task<ResponseDto> GetPendingCompanyBookingsAsync(int companyId)
        {
            // جلب الحجوزات التي حالتها بانتظار التأكيد وفلترتها حسب الشركة
            var pendingBookings = await _context.Bookings
                .Include(b => b.Customers)
                .Include(b => b.TripRoute)
                    .ThenInclude(tr => tr!.Trip)
                        .ThenInclude(t => t!.StartGovernate)
                .Include(b => b.TripRoute)
                    .ThenInclude(tr => tr!.Trip)
                        .ThenInclude(t => t!.EndGovernate)
                .Where(b => b.TripRoute != null && b.TripRoute.Trip != null && b.TripRoute.Trip.CompanyId == companyId && b.Status == BookingStatus.AwaitingConfirmation)
                .OrderByDescending(b => b.BookingAt)
                .ToListAsync();

            var bookingList = pendingBookings.Select(b => new Darb.Api.DTOs.Booking.CompanyBookingReadDto
            {
                BookingId = b.BookingId,
                TripId = b.TripRoute?.TripId ?? 0,
                TripRouteId = b.TripRouteId,
                StartGovernorate = b.TripRoute?.Trip?.StartGovernate?.Name ?? "غير محدد",
                EndGovernorate = b.TripRoute?.Trip?.EndGovernate?.Name ?? "غير محدد",
                DepartureDate = b.TripRoute?.Trip?.DepDate ?? DateTime.MinValue,
                ReservedSeatsCount = b.ReservedSeatsCount,
                TotalAmount = b.TotalAmount,
                ReceiptImagePath = !string.IsNullOrEmpty(b.ReceiptImagePath) ? _baseUrl + b.ReceiptImagePath : null,
                Status = b.Status.ToString(),
                BookingAt = b.BookingAt
            }).ToList();

            return ResponseDto.SuccessResponse($"تم استرجاع ({bookingList.Count}) حجز بانتظار التأكيد بنجاح.", bookingList);
        }

        public async Task<ResponseDto> GetBookingPassengersAsync(int bookingId, int companyId)
        {
            // التحقق أولاً من أن الحجز يتبع لرحلة تخص هذه الشركة لحماية البيانات
            var bookingCheck = await _context.Bookings
                .Include(b => b.TripRoute)
                    .ThenInclude(tr => tr!.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripRoute != null && b.TripRoute.Trip != null && b.TripRoute.Trip.CompanyId == companyId);

            if (bookingCheck == null)
            {
                return ResponseDto.FailureResponse("عذراً، لم يتم العثور على الحجز أو لا توجد صلاحية للوصول لبيانات ركابه.");
            }

            // جلب الركاب المرتبطين بهذا الحجز مع تفاصيلهم الكاملة
            var passengers = await _context.Passenger
                .Where(p => p.BookingId == bookingId)
                .ToListAsync();

            var passengerList = passengers.Select(p => new CompanyDetailedPassengerDto
            {
                PassengerId = p.PassengerId,
                FullName = p.FullName,
                NationalId = p.NationalId ?? "غير متوفر",
                PhoneNumber = p.PhoneNumber, // تأكد من مطابقة المسميات البرمجية في مودل Passenger لديك
                Address = p.Address,
                BirthDate = p.BirthDate
            }).ToList();

            return ResponseDto.SuccessResponse($"تم استرجاع بيانات ({passengerList.Count}) راكب بنجاح.", passengerList);
        }

        #endregion

        #region BankAccount Management Logic

        public async Task<ResponseDto> GetAllBankAccountsAsync(int companyId)
        {
            var users = await _context.BankAccounts
                .Include(ba => ba.Bank)
                .Where(ba => ba.CompanyId == companyId)
                .Select(ba => new BankAccountReadDto
                {
                    BankAccountId = ba.BankAccountId,
                    AccountNumber = ba.AccountNumber,
                    AccountHolderName = ba.HolderName,
                    BankId = ba.BankId,
                    BankName = ba.Bank != null ? ba.Bank.BankName : "غير متوفر",
                    CompanyId = ba.CompanyId
                }).ToListAsync();
            return ResponseDto.SuccessResponse($"تم استرجاع ({users.Count}) حساب بنكي بنجاح.", users);
        }

        public async Task<ResponseDto> GetBankAccountByIdAsync(int bankAccountId, int companyId)
        {
            var ba = await _context.BankAccounts
                .Include(b => b.Bank)
                .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId && b.CompanyId == companyId);

            if (ba == null) return ResponseDto.FailureResponse("الحساب غير موجود أو لا تملك صلاحية الوصول إليه.");

            var dto = new BankAccountReadDto
            {
                BankAccountId = ba.BankAccountId,
                AccountNumber = ba.AccountNumber,
                AccountHolderName = ba.HolderName,
                BankId = ba.BankId,
                BankName = ba.Bank?.BankName ?? "غير متوفر",
                CompanyId = ba.CompanyId
            };
            return ResponseDto.SuccessResponse("تم استرجاع الحساب بنجاح.", dto);
        }

        public async Task<ResponseDto> CreateBankAccountAsync(BankAccountCreateDto dto, int companyId)
        {
            var bankExists = await _context.Banks.AnyAsync(b => b.BankId == dto.BankId);
            if (!bankExists) return ResponseDto.FailureResponse("البنك المختار غير موجود في النظام.");

            var user = new BankAccount
            {
                AccountNumber = dto.AccountNumber,
                HolderName = dto.AccountHolderName,
                BankId = dto.BankId,
                CompanyId = companyId
            };
            await _context.BankAccounts.AddAsync(user);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم إضافة الحساب البنكي بنجاح.");
        }

        public async Task<ResponseDto> UpdateBankAccountAsync(int bankAccountId, BankAccountUpdateDto dto, int companyId)
        {
            var user = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId && b.CompanyId == companyId);
            if (user == null) return ResponseDto.FailureResponse("الحساب غير موجود.");

            if (!string.IsNullOrEmpty(dto.AccountNumber)) user.AccountNumber = dto.AccountNumber;
            if (!string.IsNullOrEmpty(dto.AccountHolderName)) user.HolderName = dto.AccountHolderName;
            if (dto.BankId.HasValue)
            {
                var bankExists = await _context.Banks.AnyAsync(b => b.BankId == dto.BankId.Value);
                if (!bankExists) return ResponseDto.FailureResponse("البنك المختار غير موجود.");
                user.BankId = dto.BankId.Value;
            }

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تحديث الحساب البنكي بنجاح.");
        }

        public async Task<ResponseDto> DeleteBankAccountAsync(int bankAccountId, int companyId)
        {
            var user = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId && b.CompanyId == companyId);
            if (user == null) return ResponseDto.FailureResponse("الحساب غير موجود.");

            _context.BankAccounts.Remove(user);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف الحساب البنكي بنجاح.");
        }

        #endregion

        #region Trip Fare Management Logic

        public async Task<ResponseDto> GetAllCompanyTripFaresAsync(int companyId)
        {
            var fares = await _context.TripFares
                .Include(tf => tf.FromGovernorate)
                .Include(tf => tf.ToGovernorate)
                .Include(tf => tf.Station)
                    .ThenInclude(s => s!.City)
                .Where(tf => tf.CompanyId == companyId)
                .Select(tf => new TripFareReadDto
                {
                    TripFareId = tf.TripFareId,
                    FromGovId = tf.FromGovId,
                    FromGovernorateName = tf.FromGovernorate != null ? tf.FromGovernorate.Name : "",
                    ToGovId = tf.ToGovId,
                    ToGovernorateName = tf.ToGovernorate != null ? tf.ToGovernorate.Name : "",
                    StationId = tf.StationId,
                    CityName = tf.Station != null && tf.Station.City != null ? tf.Station.City.Name : "",
                    Price = tf.Price,
                    IsMainStation = tf.IsMainStation,
                    CompanyId = tf.CompanyId
                }).ToListAsync();

            return ResponseDto.SuccessResponse($"تم استرجاع ({fares.Count}) تسعيرة رحلات بنجاح.", fares);
        }

        public async Task<ResponseDto> GetTripFareByIdAsync(int tripFareId, int companyId)
        {
            var tf = await _context.TripFares
                .Include(t => t.FromGovernorate)
                .Include(t => t.ToGovernorate)
                .Include(t => t.Station)
                    .ThenInclude(s => s!.City)
                .FirstOrDefaultAsync(t => t.TripFareId == tripFareId && t.CompanyId == companyId);

            if (tf == null) return ResponseDto.FailureResponse("التسعيرة غير موجودة أو لا تملك صلاحية الوصول إليها.");

            var dto = new TripFareReadDto
            {
                TripFareId = tf.TripFareId,
                FromGovId = tf.FromGovId,
                FromGovernorateName = tf.FromGovernorate?.Name ?? "غير متوفر",
                ToGovId = tf.ToGovId,
                ToGovernorateName = tf.ToGovernorate?.Name ?? "غير متوفر",
                StationId = tf.StationId,
                CityName = tf.Station?.City?.Name ?? "غير متوفر",
                Price = tf.Price,
                IsMainStation = tf.IsMainStation,
                CompanyId = tf.CompanyId
            };
            return ResponseDto.SuccessResponse("تم استرجاع التسعيرة بنجاح.", dto);
        }

        public async Task<ResponseDto> CreateTripFareAsync(CreateTripFareDto dto, int companyId)
        {
            // Verify IDs
            if (!await _context.Governorates.AnyAsync(g => g.GovernorateId == dto.FromGovId))
                return ResponseDto.FailureResponse("محافظة الانطلاق غير موجودة.");
            if (!await _context.Governorates.AnyAsync(g => g.GovernorateId == dto.ToGovId))
                return ResponseDto.FailureResponse("محافظة الوصول غير موجودة.");
            if (!await _context.Stations.AnyAsync(s => s.StationId == dto.StationId && s.CompanyId == companyId))
                return ResponseDto.FailureResponse("المحطة المختارة غير موجودة أو لا تتبع للشركة.");

            // Check if exact same mapping already exists
            bool exists = await _context.TripFares.AnyAsync(tf =>
                tf.CompanyId == companyId &&
                tf.FromGovId == dto.FromGovId &&
                tf.ToGovId == dto.ToGovId &&
                tf.StationId == dto.StationId);

            if (exists) return ResponseDto.FailureResponse("يوجد تسعيرة مسبقة لهذه الوجهة والمحطة.");

            var tripFare = new TripFare
            {
                CompanyId = companyId,
                FromGovId = dto.FromGovId,
                ToGovId = dto.ToGovId,
                StationId = dto.StationId,
                Price = dto.Price,
                IsMainStation = dto.IsMainStation,

            };

            await _context.TripFares.AddAsync(tripFare);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تمت إضافة تسعيرة المحطة بنجاح.");
        }

        public async Task<ResponseDto> UpdateTripFareAsync(int tripFareId, UpdateTripFareDto dto, int companyId)
        {
            var tf = await _context.TripFares
                .FirstOrDefaultAsync(t => t.TripFareId == tripFareId && t.CompanyId == companyId);

            if (tf == null) return ResponseDto.FailureResponse("التسعيرة غير موجودة.");

            if (dto.Price.HasValue) tf.Price = dto.Price.Value;
            dto.IsMainStation = tf.IsMainStation;


            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تحديث التسعيرة بنجاح.");
        }

        public async Task<ResponseDto> DeleteTripFareAsync(int tripFareId, int companyId)
        {
            var tf = await _context.TripFares
                .FirstOrDefaultAsync(t => t.TripFareId == tripFareId && t.CompanyId == companyId);

            if (tf == null) return ResponseDto.FailureResponse("التسعيرة غير موجودة.");

            _context.TripFares.Remove(tf);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف التسعيرة بنجاح.");
        }

        #endregion

        #region Subscription Management

        /// <summary>
        /// Returns all available subscription plans as a list of { Id, Name }.
        /// Uses EnumExtensions.GetDisplayName() to retrieve the [Display] attribute Arabic name.
        /// </summary>
        public Task<ResponseDto> GetSubscriptionPlansAsync()
        {
            // Map each enum value to an object with its integer ID and Arabic display name
            var plans = Enum.GetValues<SubscriptionPlans>()
                .Select(p => new
                {
                    Id = (int)p,
                    Name = p.GetDisplayName()
                })
                .ToList();

            return Task.FromResult(ResponseDto.SuccessResponse("تم استرجاع أنواع الاشتراك بنجاح.", plans));
        }

        /// <summary>
        /// Handles a subscription renewal request from a company.
        /// Validates and uploads the payment slip, then creates a Pending record for admin review.
        /// </summary>
        public async Task<ResponseDto> RenewSubscriptionAsync(SubscriptionRenewalDto dto)
        {
            // 1. Find the company profile by linking the provided email through the associated User account
            var company = await _context.Companies
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.User != null && c.User.Email == dto.Email.Trim());

            // Validate that the company and its corresponding user record exist
            if (company == null || company.User == null)
                return ResponseDto.FailureResponse("No transport company registered with this email address.");

            // 2. Anti-Spam Check: Prevent submitting multiple duplicate pending requests
            bool hasPendingRequest = await _context.CompanySubscription
                .AnyAsync(cs => cs.CompanyId == company.CompanyId &&
                                cs.Status == SubscriptionStatus.Pending &&
                                cs.RequestType == RequestType.Renewal);

            if (hasPendingRequest)
                return ResponseDto.FailureResponse("You already have a renewal request pending review. Please wait for admin approval.");

            // Validate that the file attachment is not null
            if (dto.PaymentSlip == null)
                return ResponseDto.FailureResponse("Please upload and attach the payment slip file.");

            // 3. Upload the uploaded payment receipt via ImageService to the designated physical folder
            string? paymentSlipPath = await _imageService.SaveImageAsync(dto.PaymentSlip, "PaymentSlips");
            if (string.IsNullOrEmpty(paymentSlipPath))
                return ResponseDto.FailureResponse("An error occurred while uploading the payment slip image.");

            // Fetch current timestamp synchronized to Yemen Timezone (UTC+3)
            var yemenNow = DateHelper.GetYemenTime();

            // 4. Smart Expiry Logic: Retrieve the latest subscription record to evaluate remaining time
            var latestSub = await _context.CompanySubscription
                .Where(cs => cs.CompanyId == company.CompanyId)
                .OrderByDescending(cs => cs.ExpiryDate)
                .FirstOrDefaultAsync();

            // If the user profile is active and the latest subscription is still valid (early renewal), cumulative extension applies.
            // Otherwise (account blocked/expired), calculation baseline drops back to the current date.
            DateTime baseStartDate = (company.User.IsActive && latestSub != null && latestSub.ExpiryDate > yemenNow)
                ? latestSub.ExpiryDate
                : yemenNow;

            // Compute the target expiry date based on the chosen contract tier plan
            DateTime expiryDate = dto.PlanType == SubscriptionPlans.Monthly
                ? baseStartDate.AddDays(30)
                : baseStartDate.AddYears(1);

            // 5. Structure and map the new pending contract record
            var subscription = new CompanySubscription
            {
                CompanyId = company.CompanyId,
                PlanType = dto.PlanType,
                PaymentSlip = paymentSlipPath,
                SubscriptionDate = yemenNow,   // Request created timestamp
                ExpiryDate = expiryDate,       // Future target coverage timeline
                Status = SubscriptionStatus.Pending,
                RequestType = RequestType.Renewal
            };

            // Commit and save changes transactionally into the SQL database state
            await _context.CompanySubscription.AddAsync(subscription);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("Your renewal request and payment receipt have been uploaded successfully. Administration will review it shortly.");
        }

      
    }
    #endregion
}

