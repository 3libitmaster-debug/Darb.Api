using Darb.Api.Dtos;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Trip;
using Darb.Api.DTOs.BankAccount;
using Darb.Api.DTOs.TripFare;
using Darb.Api.DTOs.TripSchedule;
using Darb.Api.Helpers;
using Darb.Api.Models;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace Darb.Api.Services.Implementations
{
    public class CompanyService : ICompanyService
    {
        private readonly IRepository<Trip> _tripRepository;
        private readonly IRepository<Bus> _busRepository;
        private readonly IRepository<Station> _stationRepository;
        private readonly ApplicationDbContext _context;
        private readonly IQrCodeService _qrCodeService;
   
        public CompanyService(
            IRepository<Trip> tripRepository,
            IRepository<Bus> busRepository,
            IRepository<Station> stationRepository,
            ApplicationDbContext context,
            IQrCodeService qrCodeService)
        {
            _tripRepository = tripRepository;
            _busRepository = busRepository;
            _stationRepository = stationRepository;
            _context = context;
            _qrCodeService = qrCodeService;
        }


        #region Trip Management Logic

        #region Get All Company Trips Endpoint
        /// <summary>
        /// Retrieves all trips owned by the authenticated company with related data.
        /// </summary>
        public async Task<ResponseDto> GetAllCompanyTripsAsync(int companyId)
        {
            // STEP 1: Data Retrieval
            // Fetch trips with related Bus and Governorate entities for the specified company.
            var trips = await _context.Trips
                .Include(t => t.Bus)
                .Include(t => t.StartGovernate)
                .Include(t => t.EndGovernate)
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();

            bool hasChanges = false;

            // STEP 2: Use Yemen Local Time for comparison
            // Ensuring the system logic follows Yemen's Time Zone (UTC+3) via DateHelper.
            DateTime currentYemenTime = DateHelper.GetYemenTime();

            // STEP 3: Real-time Status Synchronization Logic
            foreach (var trip in trips)
            {
                /* Logic C: Capacity-based Status Update
                   If a 'Scheduled' trip is fully booked (0 available seats).
                */
                if (trip.Status == TripStatus.scheduled && trip.AvailableSeats <= 0)
                {
                    trip.Status = TripStatus.Fulled;
                    hasChanges = true;
                }
            }

            // STEP 4: Persist changes if any statuses were updated during the loop.
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            // STEP 5: Map to DTOs for the final response.
            var tripList = trips
                .OrderByDescending(t => t.DepartureDate)
                .Select(t => new TripReadDto
                {
                    TripId = t.TripId,
                    StartGoveName = t.StartGovernate?.Name ?? "N/A",
                    EndGoveName = t.EndGovernate?.Name ?? "N/A",
                    Price = t.BasePrice,
                    DepartureDate = t.DepartureDate,
                    Status = t.Status.ToString(),
                    AvailableSeats = t.AvailableSeats,
                    BusId = t.BusId
                }).ToList();


            return ResponseDto.SuccessResponse($"تم العثور على ({tripList.Count}) رحلة بنجاح.", tripList);
        }
        #endregion

        #region Get Trip By ID Endpoint
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
                return ResponseDto.FailureResponse("نعتذر، لم يتم العثور على الرحلة المطلوبة أو قد لا تتوفر صلاحية الوصول إليها.");

            // Real-time Status Synchronization Logic
            bool statusUpdated = false;

            // Check for 'Full' status if still scheduled.
            if (trip.Status == TripStatus.scheduled && trip.AvailableSeats <= 0)
            {
                trip.Status = TripStatus.Fulled;
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
                Price = trip.BasePrice,
                Status = trip.Status.ToString(),
                AvailableSeats = trip.AvailableSeats,
                DepartureDate = trip.DepartureDate,
                BusId = trip.BusId
            };

            return ResponseDto.SuccessResponse("تم استرجاع تفاصيل الرحلة .", tripDto);
        }
        #endregion

        #region Create New Trip Endpoint
        /// <summary>
        /// Validates bus ownership and availability before creating a new scheduled trip.
        /// </summary>
        public async Task<ResponseDto> createTripAsync(CreateTripDto tripDto, int companyId)
        {
            // Business Rule: Departure time must be in the future.
            if (tripDto.DepartureDate.Date < DateHelper.GetYemenTime().Date)
                return ResponseDto.FailureResponse("عذراً، يجب أن يكون وقت انطلاق الرحلة في تاريخ مستقبلي.");

            // Validation: Ensure the bus is owned by the company making the request.
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == tripDto.BusId && b.CompanyId == companyId);

            if (bus == null)
                return ResponseDto.FailureResponse("الحافلة المختارة غير مسجلة ضمن أسطول شركتكم، يرجى التحقق من تفاصيل الحافلة.");

            // Validation: Ensure the bus status is Available.
            if (bus.Status != BusStatus.Available)
            {
                return ResponseDto.FailureResponse("عذراً، الحافلة المختارة غير متاحة للخدمة حالياً.");
            }

            if (tripDto.Routes == null || !tripDto.Routes.Any())
                return ResponseDto.FailureResponse("يجب تحديد مسارات الرحلة وأوقاتها.");

            // Fetch the fares to validate stations and get system prices.
            var tripFares = await _context.TripFares
                .Where(tf => tf.CompanyId == companyId && 
                             tf.FromGovId == tripDto.StartGoveId && 
                             tf.ToGovId == tripDto.EndGoveId)
                .ToListAsync();

            if (!tripFares.Any())
                return ResponseDto.FailureResponse("لا توجد تسعيرات مسجلة لهذا المسار، يرجى إضافة تسعيرات المحطات أولاً.");

            var primaryFare = tripFares.FirstOrDefault(tf => tf.IsMainStation == true);
            if (primaryFare == null)
                return ResponseDto.FailureResponse("يجب تحديد محطة انطلاق والمسار الرئيسي للرحلة المحددة.");

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create Entity: Assign bus capacity to available seats upon creation.
                    var trip = new Trip
                    {
                        BusId = tripDto.BusId,
                        CompanyId = companyId,
                        StartGoveId = tripDto.StartGoveId,
                        EndGoveId = tripDto.EndGoveId,
                        DepartureDate = tripDto.DepartureDate,
                        BasePrice = primaryFare.Price,
                        Status = TripStatus.scheduled,
                        AvailableSeats = bus.Capacity,
                        Period = tripDto.Period
                    };

                    await _context.Trips.AddAsync(trip);
                    await _context.SaveChangesAsync();

                    foreach (var routeDto in tripDto.Routes)
                    {
                        var matchingFare = tripFares.FirstOrDefault(tf => tf.StationId == routeDto.StationId);
                        if (matchingFare == null)
                        {
                            await transaction.RollbackAsync();
                            return ResponseDto.FailureResponse("إحدى المحطات المختارة لا ترتبط بالمسار المعرف في النظام، يرجى التأكد.");
                        }

                        var tripSchedule = new TripSchedule
                        {
                            TripId = trip.TripId,
                            StationId = matchingFare.StationId,
                            DepartureTime = routeDto.DepartureTime ?? TimeOnly.MinValue,
                            SeatFare = matchingFare.Price

                        };
                        await _context.TripSchedules.AddAsync(tripSchedule);
                    }
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var resultDto = new TripDto
                    {
                        TripId = trip.TripId,
                        BusId = trip.BusId,
                        BasePrice = trip.BasePrice,
                        DepartureDate = trip.DepartureDate,
                        Status = trip.Status.ToString()
                    };

                    return ResponseDto.SuccessResponse("تم جدولة الرحلة بنجاح.", resultDto);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var realError = ex.InnerException?.Message ?? ex.Message;
                    return ResponseDto.FailureResponse($"نعتذر، حدث خطأ تقني أثناء حفظ الرحلة: {realError}");
                }
            });
        }
        #endregion

        #region Update Trip Details Endpoint
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
            if (trip.Status == TripStatus.completed)
            {
                return ResponseDto.FailureResponse("لا يمكن تعديل بيانات هذه الرحلة نظراً لكونها مكتملة ومؤرشفة في السجلات المالية.");
            }

            // Partial Mapping: Update fields only if new values are provided in the DTO.

            if (updateDto.DepartureDate.HasValue)
            {
                if (updateDto.DepartureDate.Value.Date < DateTime.Now.Date)
                    return ResponseDto.FailureResponse("يرجى اختيار تاريخ مستقبلي؛ لا يمكن تعديل وقت الانطلاق لوقت قد مضى.");
                trip.DepartureDate = updateDto.DepartureDate.Value;
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


                var resultDto = new TripDto { TripId = trip.TripId, BasePrice = trip.BasePrice, Status = trip.Status.ToString() };
                return ResponseDto.SuccessResponse("تم تحديث بيانات الرحلة والمسارات التابعة لها بنجاح وفق التعديلات الجديدة.", resultDto);
            }
            catch (Exception ex)
            {
                var realError = ex.InnerException?.Message ?? ex.Message;
                return ResponseDto.FailureResponse($"حدث خطأ أثناء محاولة تحديث البيانات في قاعدة البيانات: {realError}");
            }
        }
        #endregion

        #region Delete Trip Endpoint
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
            if (trip.Status == TripStatus.completed)
                return ResponseDto.FailureResponse("حفاظاً على سلامة السجلات المالية والإحصائية، لا يمكن حذف الرحلات المكتملة.");

            // --- NEW: Booking Check ---
            // Check if there are any bookings associated with this trip's routes
            var hasBookings = await _context.Bookings
                .AnyAsync(b => _context.TripSchedules.Where(tr => tr.TripId == tripId).Select(tr => tr.TripScheduleId).Contains(b.TripScheduleId));

            if (hasBookings)
                return ResponseDto.FailureResponse("لا يمكن حذف هذه الرحلة لوجود حجوزات فعالة مرتبطة بمساراتها.");
            // ------------------------

            var relatedSchedules = await _context.TripSchedules.Where(tr => tr.TripId == tripId).ToListAsync();
            _context.TripSchedules.RemoveRange(relatedSchedules);

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم حذف سجل الرحلة من النظام بنجاح.");
        }
        #endregion

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
                PlateNumber = b.PlateNumber ?? "غير محدد",
                Model = b.Model ?? "غير محدد",
                Capacity = b.Capacity,
                Status = b.Status.ToString() // Converts Enum to String for the client
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
                Capacity = bus.Capacity,
                Status = bus.Status.ToString()
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
                Capacity = busDto.Capacity,
                Status = BusStatus.Available, // New buses are available by default
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
                bus.Capacity = busDto.Capacity.Value;

            if (busDto.Status.HasValue)
                bus.Status = busDto.Status.Value;


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

            // Optionally, check if the station is linked to any active trips, if needed.

            _stationRepository.Delete(station);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم حذف بيانات المحطة بنجاح.");
        }

        #endregion

        #region Booking Management Logic

        public async Task<ResponseDto> GetAllCompanyBookingsAsync(int companyId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                        .ThenInclude(t => t!.StartGovernate)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                        .ThenInclude(t => t!.EndGovernate)
                .Where(b => b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId)
                .OrderByDescending(b => b.BookingAt)
                .ToListAsync();

            var bookingList = bookings.Select(b => new Darb.Api.DTOs.Booking.CompanyBookingReadDto
            {
                BookingId = b.BookingId,
                PassengerName = b.Passenger?.FullName ?? "غير محدد",
                PhoneNumber = b.Passenger?.Phone ?? "غير محدد",
                TripId = b.TripSchedule?.TripId ?? 0,
                TripScheduleId = b.TripScheduleId,
                StartGovernorate = b.TripSchedule?.Trip?.StartGovernate?.Name ?? "غير محدد",
                EndGovernorate = b.TripSchedule?.Trip?.EndGovernate?.Name ?? "غير محدد",
                DepartureDate = b.TripSchedule?.Trip?.DepartureDate ?? DateTime.MinValue,
                NumberOfSeats = b.NumberOfSeats,
                TotalAmount = b.TotalAmount,
                ReceiptImagePath = b.ReceiptImagePath,
                Status = b.Status.ToString(),
                BookingAt = b.BookingAt
            }).ToList();

            return ResponseDto.SuccessResponse($"تم استرجاع ({bookingList.Count}) حجز بنجاح.", bookingList);
        }

        public async Task<ResponseDto> GetCompanyBookingByIdAsync(int bookingId, int companyId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Passenger)
                .Include(b => b.Passengers)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                        .ThenInclude(t => t!.StartGovernate)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                        .ThenInclude(t => t!.EndGovernate)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId);

            if (booking == null)
            {
                return ResponseDto.FailureResponse("عذراً، لم يتم العثور على الحجز أو لا توجد صلاحية للوصول إليه.");
            }

            var passengerDetailIds = booking.Passengers.Select(p => p.PassengerDetailsId).ToList();
            var eTickets = await _context.ETickets
                .Where(e => passengerDetailIds.Contains(e.PassengerDetailId))
                .ToListAsync();

            var bookingDto = new Darb.Api.DTOs.Booking.CompanyBookingDetailsDto
            {
                BookingId = booking.BookingId,
                PassengerName = booking.Passenger?.FullName ?? "غير محدد",
                PhoneNumber = booking.Passenger?.Phone ?? "غير محدد",
                TripId = booking.TripSchedule?.TripId ?? 0,
                TripScheduleId = booking.TripScheduleId,
                StartGovernorate = booking.TripSchedule?.Trip?.StartGovernate?.Name ?? "غير محدد",
                EndGovernorate = booking.TripSchedule?.Trip?.EndGovernate?.Name ?? "غير محدد",
                DepartureDate = booking.TripSchedule?.Trip?.DepartureDate ?? DateTime.MinValue,
                NumberOfSeats = booking.NumberOfSeats,
                TotalAmount = booking.TotalAmount,
                ReceiptImagePath = booking.ReceiptImagePath,
                Status = booking.Status.ToString(),
                BookingAt = booking.BookingAt,
                Passengers = booking.Passengers.Select(p =>
                {
                    var ticket = eTickets.FirstOrDefault(e => e.PassengerDetailId == p.PassengerDetailsId);
                    return new Darb.Api.DTOs.Booking.CompanyPassengerDetailDto
                    {
                        PassengerDetailId = p.PassengerDetailsId,
                        FullName = p.FullName,
                        NationalId = p.NationalId ?? "غير متوفر",
                        TicketCode = ticket?.TicketCode
                    };
                }).ToList()
            };

            return ResponseDto.SuccessResponse("تم استرجاع تفاصيل الحجز بنجاح.", bookingDto);
        }

        public async Task<ResponseDto> UpdateCompanyBookingStatusAsync(int bookingId, Darb.Api.DTOs.Booking.CompanyUpdateBookingStatusDto dto, int companyId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Passengers)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId);

            if (booking == null)
                return ResponseDto.FailureResponse("عذراً، لم يتم العثور على الحجز أو لا توجد صلاحية.");

            if (booking.Status == dto.Status)
                return ResponseDto.FailureResponse("حالة الحجز الحالية مطابقة للحالة المطلوبة.");

            booking.Status = dto.Status;

            if (dto.Status == BookingStatus.Confirmed)
            {
                foreach (var passenger in booking.Passengers)
                {
                    var existingTicket = await _context.ETickets.FirstOrDefaultAsync(e => e.PassengerDetailId == passenger.PassengerDetailsId);
                    string payload = $"BookingId:{booking.BookingId}|PassengerId:{passenger.PassengerDetailsId}";
                    string qrBase64 = _qrCodeService.GenerateQrCodeBase64(payload);

                    if (existingTicket == null)
                    {
                        var ticket = new ETicket
                        {
                            PassengerDetailId = passenger.PassengerDetailsId,
                            TicketCode = qrBase64,
                            Status = Darb.Api.Models.Enums.ETicketStatus.Active
                        };
                        await _context.ETickets.AddAsync(ticket);
                    }
                    else
                    {
                        existingTicket.TicketCode = qrBase64;
                        existingTicket.Status = Darb.Api.Models.Enums.ETicketStatus.Active;
                    }
                }
            }
            else if (dto.Status == BookingStatus.Cancelled)
            {
                var passengerDetailIds = booking.Passengers.Select(p => p.PassengerDetailsId).ToList();
                var eTickets = await _context.ETickets.Where(e => passengerDetailIds.Contains(e.PassengerDetailId)).ToListAsync();
                foreach (var ticket in eTickets)
                {
                    ticket.Status = Darb.Api.Models.Enums.ETicketStatus.Cancelled;
                }
            }

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse($"تم تحديث حالة الحجز إلى {dto.Status} بنجاح.");
        }

        public async Task<ResponseDto> DeleteCompanyBookingAsync(int bookingId, int companyId)
        {
            var booking = await _context.Bookings
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId);

            if (booking == null)
                return ResponseDto.FailureResponse("عذراً، لم يتم العثور على الحجز أو لا توجد صلاحية.");

            if (booking.Status == BookingStatus.Confirmed)
            {
                return ResponseDto.FailureResponse("لا يمكن حذف حجز مؤكد. الرجاء تغيير حالته إلى ملغى أولاً إذا لزم الأمر.");
            }

            // Must remove related passengers and their etickets before deleting booking. Or rely on cascade delete.
            // Explicit delete for safety
            var passengers = await _context.PassengerDetails.Where(pd => pd.BookingId == bookingId).ToListAsync();
            if (passengers.Any())
            {
                var passengerIds = passengers.Select(p => p.PassengerDetailsId).ToList();
                var etickets = await _context.ETickets.Where(e => passengerIds.Contains(e.PassengerDetailId)).ToListAsync();
                _context.ETickets.RemoveRange(etickets);
                _context.PassengerDetails.RemoveRange(passengers);
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم حذف الحجز نهائياً من النظام.");
        }

        public async Task<ResponseDto> ConfirmCompanyBookingClickAsync(int bookingId, int companyId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Passengers)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId);

            if (booking == null)
                return ResponseDto.FailureResponse("عذراً، لم يتم العثور على الحجز، أو لا تملك الصلاحية لتأكيده.");

            if (booking.Status == Darb.Api.Models.BookingStatus.Confirmed)
                return ResponseDto.FailureResponse("هذا الحجز تم تأكيده مسبقاً.");

            booking.Status = Darb.Api.Models.BookingStatus.Confirmed;

            foreach (var passenger in booking.Passengers)
            {
                var existingTicket = await _context.ETickets.FirstOrDefaultAsync(e => e.PassengerDetailId == passenger.PassengerDetailsId);
                int eticketId = existingTicket?.Id ?? 0; // 0 if not created yet
                int tripScheduleId = booking.TripScheduleId;
                // If ticket does not exist, we will create it and get the id after SaveChanges, but for QR, use 0 for new
                string payload = $"TripScheduleId:{tripScheduleId}|BookingId:{booking.BookingId}|ETicketId:{eticketId}|PassengerDetailsId:{passenger.PassengerDetailsId}";
                string qrBase64 = _qrCodeService.GenerateQrCodeBase64(payload);

                if (existingTicket == null)
                {
                    var ticket = new ETicket
                    {
                        PassengerDetailId = passenger.PassengerDetailsId,
                        TicketCode = qrBase64,
                        Status = Darb.Api.Models.Enums.ETicketStatus.Active
                    };
                    await _context.ETickets.AddAsync(ticket);
                }
                else
                {
                    existingTicket.TicketCode = qrBase64;
                    existingTicket.Status = Darb.Api.Models.Enums.ETicketStatus.Active;
                }
            }

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تأكيد الحجز بنجاح وتوليد تذاكر (QR Codes) للمسافرين.");
        }
        #endregion

        #region BankAccount Management Logic

        public async Task<ResponseDto> GetAllBankAccountsAsync(int companyId)
        {
            var accounts = await _context.BankAccounts
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
            return ResponseDto.SuccessResponse($"تم استرجاع ({accounts.Count}) حساب بنكي بنجاح.", accounts);
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

            var account = new BankAccount
            {
                AccountNumber = dto.AccountNumber,
                HolderName = dto.AccountHolderName,
                BankId = dto.BankId,
                CompanyId = companyId
            };
            await _context.BankAccounts.AddAsync(account);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم إضافة الحساب البنكي بنجاح.");
        }

        public async Task<ResponseDto> UpdateBankAccountAsync(int bankAccountId, BankAccountUpdateDto dto, int companyId)
        {
            var account = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId && b.CompanyId == companyId);
            if (account == null) return ResponseDto.FailureResponse("الحساب غير موجود.");

            if (!string.IsNullOrEmpty(dto.AccountNumber)) account.AccountNumber = dto.AccountNumber;
            if (!string.IsNullOrEmpty(dto.AccountHolderName)) account.HolderName = dto.AccountHolderName;
            if (dto.BankId.HasValue)
            {
                var bankExists = await _context.Banks.AnyAsync(b => b.BankId == dto.BankId.Value);
                if (!bankExists) return ResponseDto.FailureResponse("البنك المختار غير موجود.");
                account.BankId = dto.BankId.Value;
            }

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تحديث الحساب البنكي بنجاح.");
        }

        public async Task<ResponseDto> DeleteBankAccountAsync(int bankAccountId, int companyId)
        {
            var account = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId && b.CompanyId == companyId);
            if (account == null) return ResponseDto.FailureResponse("الحساب غير موجود.");

            _context.BankAccounts.Remove(account);
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
                    FromGovernorateName = tf.FromGovernorate != null ? tf.FromGovernorate.Name : "غير متوفر",
                    ToGovId = tf.ToGovId,
                    ToGovernorateName = tf.ToGovernorate != null ? tf.ToGovernorate.Name : "غير متوفر",
                    StationId = tf.StationId,
                    CityName = tf.Station != null && tf.Station.City != null ? tf.Station.City.Name : "غير متوفر",
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

        #region Trip Schedule Management

        public async Task<ResponseDto> GetAllTripSchedulesAsync(int tripId, int companyId)
        {
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);

            if (trip == null) return ResponseDto.FailureResponse("الرحلة غير موجودة أو لا تملك صلاحية الوصول إليها.");

            var schedules = await _context.TripSchedules
                .Include(ts => ts.Station)
                    .ThenInclude(s => s!.City)
                .Where(ts => ts.TripId == tripId)
                .Select(ts => new TripScheduleReadDto
                {
                    TripScheduleId = ts.TripScheduleId,
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

        public async Task<ResponseDto> GetTripScheduleByIdAsync(int scheduleId, int companyId)
        {
            var ts = await _context.TripSchedules
                .Include(ts => ts.Trip)
                .Include(ts => ts.Station)
                    .ThenInclude(s => s!.City)
                .FirstOrDefaultAsync(t => t.TripScheduleId == scheduleId && t.Trip!.CompanyId == companyId);

            if (ts == null) return ResponseDto.FailureResponse("محطة التوقف غير موجودة أو لا تملك صلاحية الوصول إليها.");

            var dto = new TripScheduleReadDto
            {
                TripScheduleId = ts.TripScheduleId,
                TripId = ts.TripId,
                StationId = ts.StationId,
                StationName = ts.Station?.Address ?? "غير متوفر",
                CityName = ts.Station?.City?.Name ?? "غير متوفر",
                DepartureTime = ts.DepartureTime,
                SeatFare = ts.SeatFare
            };

            return ResponseDto.SuccessResponse("تم استرجاع بيانات محطة التوقف بنجاح.", dto);
        }

        public async Task<ResponseDto> AddTripScheduleAsync(AddTripScheduleDto dto, int companyId)
        {
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == dto.TripId && t.CompanyId == companyId);

            if (!await _context.Stations.AnyAsync(s => s.StationId == dto.StationId && s.CompanyId == companyId))
                return ResponseDto.FailureResponse("المحطة المختارة غير موجودة أو لا تتبع لشركتكم.");

            var matchingFare = await _context.TripFares
                .FirstOrDefaultAsync(tf => tf.CompanyId == companyId && 
                                         tf.FromGovId == trip.StartGoveId && 
                                         tf.ToGovId == trip.EndGoveId && 
                                         tf.StationId == dto.StationId);

            if (matchingFare == null)
                return ResponseDto.FailureResponse("لا توجد تسعيرة معرفة لهذه المحطة على مسار هذه الرحلة.");

            try
            {
                var tripSchedule = new TripSchedule
                {
                    TripId = dto.TripId,
                    StationId = dto.StationId,
                    DepartureTime = dto.DepartureTime,
                    SeatFare = matchingFare.Price
                };

                await _context.TripSchedules.AddAsync(tripSchedule);
                await _context.SaveChangesAsync();
                return ResponseDto.SuccessResponse("تمت إضافة محطة التوقف للرحلة بنجاح.");
            }
            catch (FormatException ex)
            {
                return ResponseDto.FailureResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<ResponseDto> UpdateTripScheduleAsync(int scheduleId, UpdateTripScheduleDto dto, int companyId)
        {
            var ts = await _context.TripSchedules
                .Include(ts => ts.Trip)
                .FirstOrDefaultAsync(t => t.TripScheduleId == scheduleId && t.Trip!.CompanyId == companyId);

            if (ts == null) return ResponseDto.FailureResponse("محطة التوقف غير موجودة.");

            try
            {
                if (dto.DepartureTime.HasValue)
                    ts.DepartureTime = dto.DepartureTime.Value;

                if (dto.SeatFare.HasValue)
                    ts.SeatFare = dto.SeatFare.Value;

                await _context.SaveChangesAsync();
                return ResponseDto.SuccessResponse("تم تحديث بيانات محطة التوقف بنجاح.");
            }
            catch (FormatException ex)
            {
                return ResponseDto.FailureResponse(ex.Message);
            }
        }

        public async Task<ResponseDto> DeleteTripScheduleAsync(int scheduleId, int companyId)
        {
            var ts = await _context.TripSchedules
                .Include(ts => ts.Trip)
                .FirstOrDefaultAsync(t => t.TripScheduleId == scheduleId && t.Trip!.CompanyId == companyId);

            if (ts == null) return ResponseDto.FailureResponse("محطة التوقف غير موجودة.");

            // Check if there are bookings for this schedule
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.TripScheduleId == scheduleId);
            if (hasBookings)
            {
                return ResponseDto.FailureResponse("لا يمكن حذف هذه المحطة لوجود حجوزات مؤكدة مرتبطة بها.");
            }

            _context.TripSchedules.Remove(ts);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف محطة التوقف من الرحلة بنجاح.");
        }

        #endregion
    }
}