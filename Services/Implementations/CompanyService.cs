using Darb.Api.Dtos;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Trip;
using Darb.Api.DTOs.BankAccount;
using Darb.Api.DTOs.TripFare;
using Darb.Api.DTOs.TripSchedule;
using Darb.Api.Extensions;
using Darb.Api.Helpers;
using Darb.Api.Models;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Darb.Api.Models.Enums;
using Darb.Api.DTOs.companyDtos.Trip;
using Darb.Api.DTOs.company;
using Darb.Api.Enums;

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
   
        public CompanyService(
            IRepository<Trip> tripRepository,
            IRepository<Bus> busRepository,
            IRepository<Station> stationRepository,
            ApplicationDbContext context,
            IQrCodeService qrCodeService,
            IImageService imageService)
        {
            _tripRepository = tripRepository;
            _busRepository = busRepository;
            _stationRepository = stationRepository;
            _context = context;
            _qrCodeService = qrCodeService;
            _imageService = imageService;
        }


        #region Trip Management Logic

        #region Get All Company Trips Endpoint
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
                return ResponseDto.FailureResponse("ظ†ط¹طھط°ط±طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ط±ط­ظ„ط© ط§ظ„ظ…ط·ظ„ظˆط¨ط© ط£ظˆ ظ‚ط¯ ظ„ط§ طھطھظˆظپط± طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

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

            return ResponseDto.SuccessResponse("طھظ… ط§ط³طھط±ط¬ط§ط¹ طھظپط§طµظٹظ„ ط§ظ„ط±ط­ظ„ط© .", tripDto);
        }
        #endregion

        #region Create New Trip Endpoint
        /// <summary>
        /// Validates bus ownership and availability before creating a new scheduled trip.
        /// </summary>
        public async Task<ResponseDto> createTripAsync(CreateTripDto tripDto, int companyId)
        {
            // 1. ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ط£ظ† ط§ظ„ط¨ظٹط§ظ†ط§طھ ظ„ظ… طھطµظ„ ظپط§ط±ط؛ط©
            if (tripDto == null || tripDto.StartGoveId == 0)
            {
                return ResponseDto.FailureResponse("ط¹ط°ط±ط§ظ‹طŒ ظ„ظ… ظٹطھظ… ط§ط³طھظ„ط§ظ… ط¨ظٹط§ظ†ط§طھ ط§ظ„ط±ط­ظ„ط© ط¨ط´ظƒظ„ طµط­ظٹط­.");
            }

            var nowYemen = DateHelper.GetYemenTime();

            // 2. ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ظ…ظ†ط·ظ‚ظٹط© ط§ظ„طھط§ط±ظٹط®
            if (tripDto.DepartureDate.Date < nowYemen.Date)
            {
                return ResponseDto.FailureResponse($"ط¹ط°ط±ط§ظ‹طŒ طھط§ط±ظٹط® ط§ظ„ط±ط­ظ„ط© ({tripDto.DepartureDate:yyyy-MM-dd}) ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ظپظٹ ط§ظ„ظ…ط§ط¶ظٹ.");
            }

            // 3. ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ط­ط§ظپظ„ط© ظˆظ…ظ„ظƒظٹط© ط§ظ„ط´ط±ظƒط© ظ„ظ‡ط§
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == tripDto.BusId && b.CompanyId == companyId);

            if (bus == null)
                return ResponseDto.FailureResponse("ط§ظ„ط­ط§ظپظ„ط© ط§ظ„ظ…ط®طھط§ط±ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ط£ظˆ ط؛ظٹط± ظ…ط³ط¬ظ„ط© ظ„ط´ط±ظƒطھظƒظ….");

           

            // 4. ط¬ظ„ط¨ ط§ظ„طھط³ط¹ظٹط±ط§طھ ظˆط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ظ…ط³ط§ط± ظ„ظ„ط­طµظˆظ„ ط¹ظ„ظ‰ ط§ظ„ط³ط¹ط± ط§ظ„ط£ط³ط§ط³ظٹ
            var tripFares = await _context.TripFares
                .Where(tf => tf.CompanyId == companyId &&
                             tf.FromGovId == tripDto.StartGoveId &&
                             tf.ToGovId == tripDto.EndGoveId)
                .ToListAsync();

            if (!tripFares.Any())
                return ResponseDto.FailureResponse("ظ„ط§ طھظˆط¬ط¯ طھط³ط¹ظٹط±ط§طھ ظ…ط¹ط±ظپط© ظ„ظ‡ط°ط§ ط§ظ„ظ…ط³ط§ط± ظپظٹ ط§ظ„ظ†ط¸ط§ظ….");

            var primaryFare = tripFares.FirstOrDefault(tf => tf.IsMainStation);
            if (primaryFare == null)
                return ResponseDto.FailureResponse("ظٹط¬ط¨ طھط­ط¯ظٹط¯ ط§ظ„ط³ط¹ط± ط§ظ„ط±ط¦ظٹط³ظٹ ظ„ظ„ظ…ط³ط§ط± (Main Station) ظپظٹ ط§ظ„ط¥ط¹ط¯ط§ط¯ط§طھ.");

            // 5. ط¥ظ†ط´ط§ط، ط§ظ„ظƒط§ط¦ظ† ط§ظ„ط±ط¦ظٹط³ظٹ ظ„ظ„ط±ط­ظ„ط©
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

            return ResponseDto.SuccessResponse("طھظ… ط¥ظ†ط´ط§ط، ط§ظ„ط±ط­ظ„ط© ط¨ظ†ط¬ط§ط­. ظٹظ…ظƒظ†ظƒ ط§ظ„ط¢ظ† ط¥ط¶ط§ظپط© ط§ظ„ظ…ط³ط§ط±ط§طھ.", new { tripId = trip.TripId });
        }

        public async Task<ResponseDto> AddTripRoutesAsync(int tripId, List<RouteRequestDto> routes, int companyId)
        {
            // 1. ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ظˆط¬ظˆط¯ ط§ظ„ط±ط­ظ„ط© ظˆظ…ظ„ظƒظٹط© ط§ظ„ط´ط±ظƒط© ظ„ظ‡ط§
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);

            if (trip == null)
                return ResponseDto.FailureResponse("ط§ظ„ط±ط­ظ„ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ط£ظˆ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

            if (routes == null || !routes.Any())
                return ResponseDto.FailureResponse("ظٹط¬ط¨ طھط­ط¯ظٹط¯ ظ…ط³ط§ط±ط§طھ ط§ظ„ط±ط­ظ„ط© ظˆط£ظˆظ‚ط§طھظ‡ط§.");

            // 2. ط¬ظ„ط¨ ط§ظ„طھط³ط¹ظٹط±ط§طھ ط§ظ„ظ…طھط§ط­ط© ظ„ظ‡ط°ط§ ط§ظ„ظ…ط³ط§ط±
            var tripFares = await _context.TripFares
                .Where(tf => tf.CompanyId == companyId &&
                             tf.FromGovId == trip.StartGoveId &&
                             tf.ToGovId == trip.EndGoveId)
                .ToListAsync();

            var schedules = new List<TripSchedule>();
            foreach (var routeDto in routes)
            {
                var matchingFare = tripFares.FirstOrDefault(tf => tf.StationId == routeDto.StationId);
                if (matchingFare == null)
                {
                    return ResponseDto.FailureResponse($"ط§ظ„ظ…ط­ط·ط© ط±ظ‚ظ… {routeDto.StationId} ط؛ظٹط± ظ…ط±طھط¨ط·ط© ط¨ظ‡ط°ط§ ط§ظ„ظ…ط³ط§ط±.");
                }

                schedules.Add(new TripSchedule
                {
                    TripId = tripId,
                    StationId = routeDto.StationId,
                    DepartureTime = routeDto.DepartureTime ?? TimeOnly.MinValue,
                    SeatFare = matchingFare.Price
                });
            }

            // 3. ط¥ط¶ط§ظپط© ط§ظ„ط¬ط¯ط§ظˆظ„ ظ„ظ‚ط§ط¹ط¯ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ
            await _context.TripSchedules.AddRangeAsync(schedules);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("طھظ… ط¥ط¶ط§ظپط© ظ…ط³ط§ط±ط§طھ ط§ظ„ط±ط­ظ„ط© ط¨ظ†ط¬ط§ط­.");
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
                return ResponseDto.FailureResponse("ظ†ط£ط³ظپطŒ ط§ظ„ط±ط­ظ„ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ط£ظˆ ظ„ط§ طھظ…ظ„ظƒ ط§ظ„طµظ„ط§ط­ظٹط© ط§ظ„ظ„ط§ط²ظ…ط© ظ„طھط¹ط¯ظٹظ„ظ‡ط§.");

            // Logic: Prevent modification if the trip is completed.
            if (trip.TripStatus == TripStatus.completed)
            {
                return ResponseDto.FailureResponse("ظ„ط§ ظٹظ…ظƒظ† طھط¹ط¯ظٹظ„ ط¨ظٹط§ظ†ط§طھ ظ‡ط°ظ‡ ط§ظ„ط±ط­ظ„ط© ظ†ط¸ط±ط§ظ‹ ظ„ظƒظˆظ†ظ‡ط§ ظ…ظƒطھظ…ظ„ط© ظˆظ…ط¤ط±ط´ظپط© ظپظٹ ط§ظ„ط³ط¬ظ„ط§طھ ط§ظ„ظ…ط§ظ„ظٹط©.");
            }

            // Partial Mapping: Update fields only if new values are provided in the DTO.

            if (updateDto.DepartureDate.HasValue)
            {
                if (updateDto.DepartureDate.Value.Date < DateTime.Now.Date)
                    return ResponseDto.FailureResponse("ظٹط±ط¬ظ‰ ط§ط®طھظٹط§ط± طھط§ط±ظٹط® ظ…ط³طھظ‚ط¨ظ„ظٹط› ظ„ط§ ظٹظ…ظƒظ† طھط¹ط¯ظٹظ„ ظˆظ‚طھ ط§ظ„ط§ظ†ط·ظ„ط§ظ‚ ظ„ظˆظ‚طھ ظ‚ط¯ ظ…ط¶ظ‰.");
                trip.DepDate = updateDto.DepartureDate.Value;
            }

            // Bus Swap Validation: Ensure the new bus is also owned by this company.
            if (updateDto.BusId.HasValue && updateDto.BusId.Value != trip.BusId)
            {
                var busExists = await _context.Buses
                    .AnyAsync(b => b.BusId == updateDto.BusId.Value && b.CompanyId == companyId);

                if (!busExists)
                    return ResponseDto.FailureResponse("ط§ظ„ط­ط§ظپظ„ط© ط§ظ„ط¬ط¯ظٹط¯ط© ط§ظ„ظ…ط®طھط§ط±ط© ط؛ظٹط± طھط§ط¨ط¹ط© ظ„ط´ط±ظƒطھظƒظ…طŒ ظٹط±ط¬ظ‰ ظ…ط±ط§ط¬ط¹ط© ط¨ظٹط§ظ†ط§طھ ط§ظ„ط£ط³ط·ظˆظ„.");

                trip.BusId = updateDto.BusId.Value;
            }

            try
            {

                await _context.SaveChangesAsync();


                var resultDto = new TripDto { TripId = trip.TripId, BasePrice = trip.Price, Status = trip.TripStatus.ToString() };
                return ResponseDto.SuccessResponse("طھظ… طھط­ط¯ظٹط« ط¨ظٹط§ظ†ط§طھ ط§ظ„ط±ط­ظ„ط© ظˆط§ظ„ظ…ط³ط§ط±ط§طھ ط§ظ„طھط§ط¨ط¹ط© ظ„ظ‡ط§ ط¨ظ†ط¬ط§ط­ ظˆظپظ‚ ط§ظ„طھط¹ط¯ظٹظ„ط§طھ ط§ظ„ط¬ط¯ظٹط¯ط©.", resultDto);
            }
            catch (Exception ex)
            {
                var realError = ex.InnerException?.Message ?? ex.Message;
                return ResponseDto.FailureResponse($"ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، ظ…ط­ط§ظˆظ„ط© طھط­ط¯ظٹط« ط§ظ„ط¨ظٹط§ظ†ط§طھ ظپظٹ ظ‚ط§ط¹ط¯ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ: {realError}");
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
                return ResponseDto.FailureResponse("ظ†ط¹طھط°ط±طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ط±ط­ظ„ط© ط§ظ„ظ…ط±ط§ط¯ ط­ط°ظپظ‡ط§.");

            // Safety Rule: Completed trips should remain in history and cannot be deleted.
            if (trip.TripStatus == TripStatus.completed)
                return ResponseDto.FailureResponse("ط­ظپط§ط¸ط§ظ‹ ط¹ظ„ظ‰ ط³ظ„ط§ظ…ط© ط§ظ„ط³ط¬ظ„ط§طھ ط§ظ„ظ…ط§ظ„ظٹط© ظˆط§ظ„ط¥ط­طµط§ط¦ظٹط©طŒ ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ط§ظ„ط±ط­ظ„ط§طھ ط§ظ„ظ…ظƒطھظ…ظ„ط©.");

            // --- NEW: Booking Check ---
            // Check if there are any bookings associated with this trip's routes
            var hasBookings = await _context.Bookings
                .AnyAsync(b => _context.TripSchedules.Where(tr => tr.TripId == tripId).Select(tr => tr.TripScheduleId).Contains(b.TripScheduleId));

            if (hasBookings)
                return ResponseDto.FailureResponse("ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ظ‡ط°ظ‡ ط§ظ„ط±ط­ظ„ط© ظ„ظˆط¬ظˆط¯ ط­ط¬ظˆط²ط§طھ ظپط¹ط§ظ„ط© ظ…ط±طھط¨ط·ط© ط¨ظ…ط³ط§ط±ط§طھظ‡ط§.");
            // ------------------------

            var relatedSchedules = await _context.TripSchedules.Where(tr => tr.TripId == tripId).ToListAsync();
            _context.TripSchedules.RemoveRange(relatedSchedules);

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("طھظ… ط­ط°ظپ ط³ط¬ظ„ ط§ظ„ط±ط­ظ„ط© ظ…ظ† ط§ظ„ظ†ط¸ط§ظ… ط¨ظ†ط¬ط§ط­.");
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
                PlateNumber = b.PlateNumber ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                Model = b.Model ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                Capacity = b.BusCapacity,
                Status = b.BusStatus.ToString() // Converts Enum to String for the client
            }).ToList();

            return ResponseDto.SuccessResponse($"طھظ… ط§ط³طھط¹ط§ط¯ط© ط¨ظٹط§ظ†ط§طھ ط§ظ„ط£ط³ط·ظˆظ„ ط¨ظ†ط¬ط§ط­طŒ ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ط­ط§ظپظ„ط§طھ: {busList.Count}", busList);
        }

        /// <summary>
        /// Retrieves details of a specific bus, ensuring it belongs to the authenticated company.
        /// </summary>
        public async Task<ResponseDto> GetBusByIdAsync(int busId, int companyId)
        {
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == busId && b.CompanyId == companyId);

            if (bus == null)
                return ResponseDto.FailureResponse("ظ†ط¹طھط°ط±طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ط­ط§ظپظ„ط© ط§ظ„ظ…ط·ظ„ظˆط¨ط©طŒ ط£ظˆ ظ‚ط¯ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

            var busDto = new BusReadDto
            {
                BusId = bus.BusId,
                PlateNumber = bus.PlateNumber ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                Model = bus.Model ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                Capacity = bus.BusCapacity,
                Status = bus.BusStatus.ToString()
            };

            return ResponseDto.SuccessResponse("طھظ… ط§ط³طھط±ط¬ط§ط¹ ط¨ظٹط§ظ†ط§طھ ط§ظ„ط­ط§ظپظ„ط© ط¨ظ†ط¬ط§ط­.", busDto);
        }

        /// <summary>
        /// Registers a new bus into the company's fleet using the Generic Repository.
        /// </summary>
        public async Task<ResponseDto> CreateBusAsync(CreateBusDto busDto, int companyId)
        {
            // Business Rule: Ensure plate numbers are unique across the system
            var exists = await _context.Buses.AnyAsync(b => b.PlateNumber == busDto.PlateNumber);
            if (exists)
                return ResponseDto.FailureResponse("ط¹ط°ط±ط§ظ‹طŒ ط±ظ‚ظ… ط§ظ„ظ„ظˆط­ط© ظ‡ط°ط§ ظ…ط³ط¬ظ„ ظ…ط³ط¨ظ‚ط§ظ‹ ظپظٹ ط§ظ„ظ†ط¸ط§ظ…طŒ ظٹط±ط¬ظ‰ ط§ظ„طھط£ظƒط¯ ظ…ظ† طµط­ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ.");

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

            return ResponseDto.SuccessResponse("طھظ… طھط³ط¬ظٹظ„ ط§ظ„ط­ط§ظپظ„ط© ط§ظ„ط¬ط¯ظٹط¯ط© ط¨ظ†ط¬ط§ط­ ظˆط§ظ†ط¶ظ…ط§ظ…ظ‡ط§ ظ„ط£ط³ط·ظˆظ„ ط´ط±ظƒطھظƒظ….");
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
                return ResponseDto.FailureResponse("ظ†ط¹طھط°ط±طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ط­ط§ظپظ„ط© ط§ظ„ظ…ط·ظ„ظˆط¨ط© ط£ظˆ ظ‚ط¯ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

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

            return ResponseDto.SuccessResponse("طھظ… طھط­ط¯ظٹط« ط¨ظٹط§ظ†ط§طھ ط§ظ„ط­ط§ظپظ„ط© ظپظٹ ط§ظ„ظ†ط¸ط§ظ… ط¨ظ†ط¬ط§ط­.");
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
                return ResponseDto.FailureResponse("ط¹ط°ط±ط§ظ‹طŒ ط§ظ„ط­ط§ظپظ„ط© ط§ظ„ظ…ط±ط§ط¯ ط­ط°ظپظ‡ط§ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ظپظٹ ط³ط¬ظ„ط§طھظƒظ….");

            // Critical Rule: Prevent deletion if there is a trip history (Referential Integrity)
            if (bus.Trip != null && bus.Trip.Any())
            {
                return ResponseDto.FailureResponse("ط­ظپط§ط¸ط§ظ‹ ط¹ظ„ظ‰ ط³ظ„ط§ظ…ط© ط§ظ„ط³ط¬ظ„ط§طھ ط§ظ„طھط§ط±ظٹط®ظٹط© ظ„ظ„ط±ط­ظ„ط§طھطŒ ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ط­ط§ظپظ„ط© ظ…ط±طھط¨ط·ط© ط¨ط¹ظ…ظ„ظٹط§طھ ط³ط§ط¨ظ‚ط©طŒ ظ†ظˆطµظٹ ط¨طھط؛ظٹظٹط± ط­ط§ظ„طھظ‡ط§ ط¥ظ„ظ‰ 'ط®ط§ط±ط¬ ط§ظ„ط®ط¯ظ…ط©' ط¨ط¯ظ„ط§ظ‹ ظ…ظ† ط§ظ„ط­ط°ظپ.");
            }

            // Perform permanent deletion via Generic Repository
            _busRepository.Delete(bus);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("طھظ… ط­ط°ظپ ط³ط¬ظ„ ط§ظ„ط­ط§ظپظ„ط© ظ…ظ† ط§ظ„ظ†ط¸ط§ظ… ط¨ط´ظƒظ„ ظ†ظ‡ط§ط¦ظٹ.");
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
                Address = s.Address ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                CityId = s.CityId,
                CityName = s.City?.Name ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                GovernorateId = s.GovernorateId,
                GovernorateName = s.Governorate?.Name ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                CompanyId = s.CompanyId
            }).ToList();

            return ResponseDto.SuccessResponse($"طھظ… ط§ط³طھط±ط¬ط§ط¹ ط§ظ„ظ…ط­ط·ط§طھ ط¨ظ†ط¬ط§ط­طŒ ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط­ط·ط§طھ: {stationList.Count}", stationList);
        }

        public async Task<ResponseDto> GetStationByIdAsync(int stationId, int companyId)
        {
            var station = await _context.Stations
                .Include(s => s.City)
                .Include(s => s.Governorate)
                .FirstOrDefaultAsync(s => s.StationId == stationId && s.CompanyId == companyId);

            if (station == null)
                return ResponseDto.FailureResponse("ظ†ط¹طھط°ط±طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ظ…ط­ط·ط© ط§ظ„ظ…ط·ظ„ظˆط¨ط©طŒ ط£ظˆ ظ‚ط¯ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

            var stationDto = new Darb.Api.DTOs.Station.StationReadDto
            {
                StationId = station.StationId,
                Address = station.Address ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                CityId = station.CityId,
                CityName = station.City?.Name ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                GovernorateId = station.GovernorateId,
                GovernorateName = station.Governorate?.Name ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                CompanyId = station.CompanyId
            };

            return ResponseDto.SuccessResponse("طھظ… ط§ط³طھط±ط¬ط§ط¹ ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…ط­ط·ط© ط¨ظ†ط¬ط§ط­.", stationDto);
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

            return ResponseDto.SuccessResponse("طھظ… ط¥ط¶ط§ظپط© ط§ظ„ظ…ط­ط·ط© ط§ظ„ط¬ط¯ظٹط¯ط© ط¨ظ†ط¬ط§ط­ ظˆط§ظ†ط¶ظ…ط§ظ…ظ‡ط§ ظ„ط´ط¨ظƒط© ط§ظ„ظ…ط­ط·ط§طھ.");
        }

        public async Task<ResponseDto> UpdateStationAsync(int stationId, Darb.Api.DTOs.Station.UpdateStationDto stationDto, int companyId)
        {
            var station = await _context.Stations
                .FirstOrDefaultAsync(s => s.StationId == stationId && s.CompanyId == companyId);

            if (station == null)
                return ResponseDto.FailureResponse("ظ†ط¹طھط°ط±طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ظ…ط­ط·ط© ط§ظ„ظ…ط·ظ„ظˆط¨ط© ط£ظˆ ظ‚ط¯ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

            if (!string.IsNullOrEmpty(stationDto.Address))
                station.Address = stationDto.Address;

            if (stationDto.CityId.HasValue)
                station.CityId = stationDto.CityId.Value;

            if (stationDto.GovernorateId.HasValue)
                station.GovernorateId = stationDto.GovernorateId.Value;

            _stationRepository.Update(station);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("طھظ… طھط­ط¯ظٹط« ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…ط­ط·ط© ط¨ظ†ط¬ط§ط­.");
        }

        public async Task<ResponseDto> DeleteStationAsync(int stationId, int companyId)
        {
            var station = await _context.Stations
                .FirstOrDefaultAsync(s => s.StationId == stationId && s.CompanyId == companyId);

            if (station == null)
                return ResponseDto.FailureResponse("ط¹ط°ط±ط§ظ‹طŒ ط§ظ„ظ…ط­ط·ط© ط§ظ„ظ…ط±ط§ط¯ ط­ط°ظپظ‡ط§ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©.");

            // Optionally, check if the station is linked to any active trips, if needed.

            _stationRepository.Delete(station);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("طھظ… ط­ط°ظپ ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…ط­ط·ط© ط¨ظ†ط¬ط§ط­.");
        }

        #endregion

        #region Booking Management Logic

        public async Task<ResponseDto> GetAllCompanyBookingsAsync(int companyId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
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
                PassengerName = b.Customer?.FullName ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                PhoneNumber = b.Customer?.Phone ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                TripId = b.TripSchedule?.TripId ?? 0,
                TripScheduleId = b.TripScheduleId,
                StartGovernorate = b.TripSchedule?.Trip?.StartGovernate?.Name ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                EndGovernorate = b.TripSchedule?.Trip?.EndGovernate?.Name ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                DepartureDate = b.TripSchedule?.Trip?.DepDate ?? DateTime.MinValue,
                ReservedSeatsCount = b.ReservedSeatsCount,
                TotalAmount = b.TotalAmount,
                ReceiptImagePath = b.ReceiptImagePath,
                Status = b.Status.ToString(),
                BookingAt = b.BookingAt
            }).ToList();

            return ResponseDto.SuccessResponse($"طھظ… ط§ط³طھط±ط¬ط§ط¹ ({bookingList.Count}) ط­ط¬ط² ط¨ظ†ط¬ط§ط­.", bookingList);
        }

        public async Task<ResponseDto> GetCompanyBookingByIdAsync(int bookingId, int companyId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Customers)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                        .ThenInclude(t => t!.StartGovernate)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                        .ThenInclude(t => t!.EndGovernate)
                .Include(b => b.ETicket)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId);

            if (booking == null)
            {
                return ResponseDto.FailureResponse("ط¹ط°ط±ط§ظ‹طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ط­ط¬ط² ط£ظˆ ظ„ط§ طھظˆط¬ط¯ طµظ„ط§ط­ظٹط© ظ„ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡.");
            }


            var bookingDto = new Darb.Api.DTOs.Booking.CompanyBookingDetailsDto
            {
                BookingId = booking.BookingId,
                PassengerName = booking.Customer?.FullName ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                PhoneNumber = booking.Customer?.Phone ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                TripId = booking.TripSchedule?.TripId ?? 0,
                TripScheduleId = booking.TripScheduleId,
                StartGovernorate = booking.TripSchedule?.Trip?.StartGovernate?.Name ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                EndGovernorate = booking.TripSchedule?.Trip?.EndGovernate?.Name ?? "ط؛ظٹط± ظ…ط­ط¯ط¯",
                DepartureDate = booking.TripSchedule?.Trip?.DepDate ?? DateTime.MinValue,
                ReservedSeatsCount = booking.ReservedSeatsCount,
                TotalAmount = booking.TotalAmount,
                ReceiptImagePath = booking.ReceiptImagePath,
                Status = booking.Status.ToString(),
                BookingAt = booking.BookingAt,
                TicketCode = booking.ETicket?.TicketCode,
                Customers = booking.Customers.Select(p => new Darb.Api.DTOs.Booking.CompanyPassengerDetailDto
                {
                    PassengerDetailId = p.PassengerId,
                    FullName = p.FullName,
                    NationalId = p.NationalId ?? "ط؛ظٹط± ظ…طھظˆظپط±",
                }).ToList()
            };

            return ResponseDto.SuccessResponse("طھظ… ط§ط³طھط±ط¬ط§ط¹ طھظپط§طµظٹظ„ ط§ظ„ط­ط¬ط² ط¨ظ†ط¬ط§ط­.", bookingDto);
        }

        public async Task<ResponseDto> UpdateCompanyBookingStatusAsync(int bookingId, Darb.Api.DTOs.Booking.CompanyUpdateBookingStatusDto dto, int companyId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customers)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId);

            if (booking == null)
                return ResponseDto.FailureResponse("ط¹ط°ط±ط§ظ‹طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ط­ط¬ط² ط£ظˆ ظ„ط§ طھظˆط¬ط¯ طµظ„ط§ط­ظٹط©.");

            if (booking.Status == dto.Status)
                return ResponseDto.FailureResponse("ط­ط§ظ„ط© ط§ظ„ط­ط¬ط² ط§ظ„ط­ط§ظ„ظٹط© ظ…ط·ط§ط¨ظ‚ط© ظ„ظ„ط­ط§ظ„ط© ط§ظ„ظ…ط·ظ„ظˆط¨ط©.");

            booking.Status = dto.Status;

            if (dto.Status == BookingStatus.Confirmed)
            {
                var existingTicket = await _context.ETickets.FirstOrDefaultAsync(e => e.BookingId == booking.BookingId);
                string payload = $"BookingId:{booking.BookingId}|TripScheduleId:{booking.TripScheduleId}";
                string qrBase64 = _qrCodeService.GenerateQrCodeBase64(payload);

                if (existingTicket == null)
                {
                    var ticket = new ETicket
                    {
                        BookingId = booking.BookingId,
                        TicketCode = qrBase64,
                        Status = Darb.Api.Models.Enums.ETicketStatus.Valid
                    };
                    await _context.ETickets.AddAsync(ticket);
                }
                else
                {
                    existingTicket.TicketCode = qrBase64;
                    existingTicket.Status = Darb.Api.Models.Enums.ETicketStatus.Valid;
                }
            }
            else if (dto.Status == BookingStatus.Cancelled)
            {
                var ticket = await _context.ETickets.FirstOrDefaultAsync(e => e.BookingId == booking.BookingId);
                if (ticket != null)
                {
                    ticket.Status = ETicketStatus.UnValid;
                }
            }

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("طھظ… طھط£ظƒظٹط¯ طھط­ط¯ظٹط« ط­ط§ظ„ط© ط§ظ„ط­ط¬ط² ط¨ظ†ط¬ط§ط­.");
        }

        public async Task<ResponseDto> DeleteCompanyBookingAsync(int bookingId, int companyId)
        {
            var booking = await _context.Bookings
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId);

            if (booking == null)
                return ResponseDto.FailureResponse("ط¹ط°ط±ط§ظ‹طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ط­ط¬ط² ط£ظˆ ظ„ط§ طھظˆط¬ط¯ طµظ„ط§ط­ظٹط©.");

            if (booking.Status == BookingStatus.Confirmed)
            {
                return ResponseDto.FailureResponse("ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ط­ط¬ط² ظ…ط¤ظƒط¯. ط§ظ„ط±ط¬ط§ط، طھط؛ظٹظٹط± ط­ط§ظ„طھظ‡ ط¥ظ„ظ‰ ظ…ظ„ط؛ظ‰ ط£ظˆظ„ط§ظ‹ ط¥ط°ط§ ظ„ط²ظ… ط§ظ„ط£ظ…ط±.");
            }

            // Must remove related customers and their etickets before deleting booking. Or rely on cascade delete.
            // Explicit delete for safety
            var customers = await _context.Passenger.Where(pd => pd.BookingId == bookingId).ToListAsync();
            if (customers.Any())
            {
                _context.Passenger.RemoveRange(customers);
            }
            
            var ticket = await _context.ETickets.FirstOrDefaultAsync(e => e.BookingId == bookingId);
            if (ticket != null)
            {
                _context.ETickets.Remove(ticket);
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("طھظ… ط­ط°ظپ ط§ظ„ط­ط¬ط² ظ†ظ‡ط§ط¦ظٹط§ظ‹ ظ…ظ† ط§ظ„ظ†ط¸ط§ظ….");
        }

        public async Task<ResponseDto> ConfirmCompanyBookingClickAsync(int bookingId, int companyId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customers)
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.TripSchedule != null && b.TripSchedule.Trip != null && b.TripSchedule.Trip.CompanyId == companyId);

            if (booking == null)
                return ResponseDto.FailureResponse("ط¹ط°ط±ط§ظ‹طŒ ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط§ظ„ط­ط¬ط²طŒ ط£ظˆ ظ„ط§ طھظ…ظ„ظƒ ط§ظ„طµظ„ط§ط­ظٹط© ظ„طھط£ظƒظٹط¯ظ‡.");

            if (booking.Status == BookingStatus.Confirmed)
                return ResponseDto.FailureResponse("ظ‡ط°ط§ ط§ظ„ط­ط¬ط² طھظ… طھط£ظƒظٹط¯ظ‡ ظ…ط³ط¨ظ‚ط§ظ‹.");

            booking.Status = BookingStatus.Confirmed;

            var existingTicket = await _context.ETickets.FirstOrDefaultAsync(e => e.BookingId == booking.BookingId);
            int eticketId = existingTicket?.Id ?? 0;
            int tripScheduleId = booking.TripScheduleId;

            string payload = $"TripScheduleId:{tripScheduleId}|BookingId:{booking.BookingId}|ETicketId:{eticketId}";
            string qrBase64 = _qrCodeService.GenerateQrCodeBase64(payload);

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

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("طھظ… طھط£ظƒظٹط¯ ط§ظ„ط­ط¬ط² ط¨ظ†ط¬ط§ط­!");
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
                    BankName = ba.Bank != null ? ba.Bank.BankName : "ط؛ظٹط± ظ…طھظˆظپط±",
                    CompanyId = ba.CompanyId
                }).ToListAsync();
            return ResponseDto.SuccessResponse($"طھظ… ط§ط³طھط±ط¬ط§ط¹ ({users.Count}) ط­ط³ط§ط¨ ط¨ظ†ظƒظٹ ط¨ظ†ط¬ط§ط­.", users);
        }

        public async Task<ResponseDto> GetBankAccountByIdAsync(int bankAccountId, int companyId)
        {
            var ba = await _context.BankAccounts
                .Include(b => b.Bank)
                .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId && b.CompanyId == companyId);

            if (ba == null) return ResponseDto.FailureResponse("ط§ظ„ط­ط³ط§ط¨ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ ط£ظˆ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡.");

            var dto = new BankAccountReadDto
            {
                BankAccountId = ba.BankAccountId,
                AccountNumber = ba.AccountNumber,
                AccountHolderName = ba.HolderName,
                BankId = ba.BankId,
                BankName = ba.Bank?.BankName ?? "ط؛ظٹط± ظ…طھظˆظپط±",
                CompanyId = ba.CompanyId
            };
            return ResponseDto.SuccessResponse("طھظ… ط§ط³طھط±ط¬ط§ط¹ ط§ظ„ط­ط³ط§ط¨ ط¨ظ†ط¬ط§ط­.", dto);
        }

        public async Task<ResponseDto> CreateBankAccountAsync(BankAccountCreateDto dto, int companyId)
        {
            var bankExists = await _context.Banks.AnyAsync(b => b.BankId == dto.BankId);
            if (!bankExists) return ResponseDto.FailureResponse("ط§ظ„ط¨ظ†ظƒ ط§ظ„ظ…ط®طھط§ط± ط؛ظٹط± ظ…ظˆط¬ظˆط¯ ظپظٹ ط§ظ„ظ†ط¸ط§ظ….");

            var user = new BankAccount
            {
                AccountNumber = dto.AccountNumber,
                HolderName = dto.AccountHolderName,
                BankId = dto.BankId,
                CompanyId = companyId
            };
            await _context.BankAccounts.AddAsync(user);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("طھظ… ط¥ط¶ط§ظپط© ط§ظ„ط­ط³ط§ط¨ ط§ظ„ط¨ظ†ظƒظٹ ط¨ظ†ط¬ط§ط­.");
        }

        public async Task<ResponseDto> UpdateBankAccountAsync(int bankAccountId, BankAccountUpdateDto dto, int companyId)
        {
            var user = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId && b.CompanyId == companyId);
            if (user == null) return ResponseDto.FailureResponse("ط§ظ„ط­ط³ط§ط¨ ط؛ظٹط± ظ…ظˆط¬ظˆط¯.");

            if (!string.IsNullOrEmpty(dto.AccountNumber)) user.AccountNumber = dto.AccountNumber;
            if (!string.IsNullOrEmpty(dto.AccountHolderName)) user.HolderName = dto.AccountHolderName;
            if (dto.BankId.HasValue)
            {
                var bankExists = await _context.Banks.AnyAsync(b => b.BankId == dto.BankId.Value);
                if (!bankExists) return ResponseDto.FailureResponse("ط§ظ„ط¨ظ†ظƒ ط§ظ„ظ…ط®طھط§ط± ط؛ظٹط± ظ…ظˆط¬ظˆط¯.");
                user.BankId = dto.BankId.Value;
            }

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("طھظ… طھط­ط¯ظٹط« ط§ظ„ط­ط³ط§ط¨ ط§ظ„ط¨ظ†ظƒظٹ ط¨ظ†ط¬ط§ط­.");
        }

        public async Task<ResponseDto> DeleteBankAccountAsync(int bankAccountId, int companyId)
        {
            var user = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId && b.CompanyId == companyId);
            if (user == null) return ResponseDto.FailureResponse("ط§ظ„ط­ط³ط§ط¨ ط؛ظٹط± ظ…ظˆط¬ظˆط¯.");

            _context.BankAccounts.Remove(user);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("طھظ… ط­ط°ظپ ط§ظ„ط­ط³ط§ط¨ ط§ظ„ط¨ظ†ظƒظٹ ط¨ظ†ط¬ط§ط­.");
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
                    FromGovernorateName = tf.FromGovernorate != null ? tf.FromGovernorate.Name : "ط؛ظٹط± ظ…طھظˆظپط±",
                    ToGovId = tf.ToGovId,
                    ToGovernorateName = tf.ToGovernorate != null ? tf.ToGovernorate.Name : "ط؛ظٹط± ظ…طھظˆظپط±",
                    StationId = tf.StationId,
                    CityName = tf.Station != null && tf.Station.City != null ? tf.Station.City.Name : "ط؛ظٹط± ظ…طھظˆظپط±",
                    Price = tf.Price,
                    IsMainStation = tf.IsMainStation,
                    CompanyId = tf.CompanyId
                }).ToListAsync();

            return ResponseDto.SuccessResponse($"طھظ… ط§ط³طھط±ط¬ط§ط¹ ({fares.Count}) طھط³ط¹ظٹط±ط© ط±ط­ظ„ط§طھ ط¨ظ†ط¬ط§ط­.", fares);
        }

        public async Task<ResponseDto> GetTripFareByIdAsync(int tripFareId, int companyId)
        {
            var tf = await _context.TripFares
                .Include(t => t.FromGovernorate)
                .Include(t => t.ToGovernorate)
                .Include(t => t.Station)
                    .ThenInclude(s => s!.City)
                .FirstOrDefaultAsync(t => t.TripFareId == tripFareId && t.CompanyId == companyId);

            if (tf == null) return ResponseDto.FailureResponse("ط§ظ„طھط³ط¹ظٹط±ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ط£ظˆ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

            var dto = new TripFareReadDto
            {
                TripFareId = tf.TripFareId,
                FromGovId = tf.FromGovId,
                FromGovernorateName = tf.FromGovernorate?.Name ?? "ط؛ظٹط± ظ…طھظˆظپط±",
                ToGovId = tf.ToGovId,
                ToGovernorateName = tf.ToGovernorate?.Name ?? "ط؛ظٹط± ظ…طھظˆظپط±",
                StationId = tf.StationId,
                CityName = tf.Station?.City?.Name ?? "ط؛ظٹط± ظ…طھظˆظپط±",
                Price = tf.Price,
                IsMainStation = tf.IsMainStation,
                CompanyId = tf.CompanyId
            };
            return ResponseDto.SuccessResponse("طھظ… ط§ط³طھط±ط¬ط§ط¹ ط§ظ„طھط³ط¹ظٹط±ط© ط¨ظ†ط¬ط§ط­.", dto);
        }

        public async Task<ResponseDto> CreateTripFareAsync(CreateTripFareDto dto, int companyId)
        {
            // Verify IDs
            if (!await _context.Governorates.AnyAsync(g => g.GovernorateId == dto.FromGovId))
                return ResponseDto.FailureResponse("ظ…ط­ط§ظپط¸ط© ط§ظ„ط§ظ†ط·ظ„ط§ظ‚ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©.");
            if (!await _context.Governorates.AnyAsync(g => g.GovernorateId == dto.ToGovId))
                return ResponseDto.FailureResponse("ظ…ط­ط§ظپط¸ط© ط§ظ„ظˆطµظˆظ„ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©.");
            if (!await _context.Stations.AnyAsync(s => s.StationId == dto.StationId && s.CompanyId == companyId))
                return ResponseDto.FailureResponse("ط§ظ„ظ…ط­ط·ط© ط§ظ„ظ…ط®طھط§ط±ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ط£ظˆ ظ„ط§ طھطھط¨ط¹ ظ„ظ„ط´ط±ظƒط©.");

            // Check if exact same mapping already exists
            bool exists = await _context.TripFares.AnyAsync(tf => 
                tf.CompanyId == companyId && 
                tf.FromGovId == dto.FromGovId && 
                tf.ToGovId == dto.ToGovId && 
                tf.StationId == dto.StationId);
            
            if (exists) return ResponseDto.FailureResponse("ظٹظˆط¬ط¯ طھط³ط¹ظٹط±ط© ظ…ط³ط¨ظ‚ط© ظ„ظ‡ط°ظ‡ ط§ظ„ظˆط¬ظ‡ط© ظˆط§ظ„ظ…ط­ط·ط©.");

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
            return ResponseDto.SuccessResponse("طھظ…طھ ط¥ط¶ط§ظپط© طھط³ط¹ظٹط±ط© ط§ظ„ظ…ط­ط·ط© ط¨ظ†ط¬ط§ط­.");
        }

        public async Task<ResponseDto> UpdateTripFareAsync(int tripFareId, UpdateTripFareDto dto, int companyId)
        {
            var tf = await _context.TripFares
                .FirstOrDefaultAsync(t => t.TripFareId == tripFareId && t.CompanyId == companyId);
            
            if (tf == null) return ResponseDto.FailureResponse("ط§ظ„طھط³ط¹ظٹط±ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©.");

            if (dto.Price.HasValue) tf.Price = dto.Price.Value;
            dto.IsMainStation = tf.IsMainStation;


            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("طھظ… طھط­ط¯ظٹط« ط§ظ„طھط³ط¹ظٹط±ط© ط¨ظ†ط¬ط§ط­.");
        }

        public async Task<ResponseDto> DeleteTripFareAsync(int tripFareId, int companyId)
        {
            var tf = await _context.TripFares
                .FirstOrDefaultAsync(t => t.TripFareId == tripFareId && t.CompanyId == companyId);

            if (tf == null) return ResponseDto.FailureResponse("ط§ظ„طھط³ط¹ظٹط±ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©.");

            _context.TripFares.Remove(tf);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("طھظ… ط­ط°ظپ ط§ظ„طھط³ط¹ظٹط±ط© ط¨ظ†ط¬ط§ط­.");
        }

        #endregion

        #region Trip Schedule Management

        public async Task<ResponseDto> GetAllTripSchedulesAsync(int tripId, int companyId)
        {
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.CompanyId == companyId);

            if (trip == null) return ResponseDto.FailureResponse("ط§ظ„ط±ط­ظ„ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ط£ظˆ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

            var schedules = await _context.TripSchedules
                .Include(ts => ts.Station)
                    .ThenInclude(s => s!.City)
                .Where(ts => ts.TripId == tripId)
                .Select(ts => new TripScheduleReadDto
                {
                    TripScheduleId = ts.TripScheduleId,
                    TripId = ts.TripId,
                    StationId = ts.StationId,
                    StationName = ts.Station != null ? ts.Station.Address : "ط؛ظٹط± ظ…طھظˆظپط±",
                    CityName = ts.Station != null && ts.Station.City != null ? ts.Station.City.Name : "ط؛ظٹط± ظ…طھظˆظپط±",
                    DepartureTime = ts.DepartureTime,
                    SeatFare = ts.SeatFare
                })
                .ToListAsync();

            return ResponseDto.SuccessResponse($"طھظ… ط§ط³طھط±ط¬ط§ط¹ ({schedules.Count}) ظ…ط­ط·ط§طھ طھظˆظ‚ظپ ظ„ظ„ط±ط­ظ„ط© ط¨ظ†ط¬ط§ط­.", schedules);
        }

        public async Task<ResponseDto> GetTripScheduleByIdAsync(int scheduleId, int companyId)
        {
            var ts = await _context.TripSchedules
                .Include(ts => ts.Trip)
                .Include(ts => ts.Station)
                    .ThenInclude(s => s!.City)
                .FirstOrDefaultAsync(t => t.TripScheduleId == scheduleId && t.Trip!.CompanyId == companyId);

            if (ts == null) return ResponseDto.FailureResponse("ظ…ط­ط·ط© ط§ظ„طھظˆظ‚ظپ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ط£ظˆ ظ„ط§ طھظ…ظ„ظƒ طµظ„ط§ط­ظٹط© ط§ظ„ظˆطµظˆظ„ ط¥ظ„ظٹظ‡ط§.");

            var dto = new TripScheduleReadDto
            {
                TripScheduleId = ts.TripScheduleId,
                TripId = ts.TripId,
                StationId = ts.StationId,
                StationName = ts.Station?.Address ?? "ط؛ظٹط± ظ…طھظˆظپط±",
                CityName = ts.Station?.City?.Name ?? "ط؛ظٹط± ظ…طھظˆظپط±",
                DepartureTime = ts.DepartureTime,
                SeatFare = ts.SeatFare
            };

            return ResponseDto.SuccessResponse("طھظ… ط§ط³طھط±ط¬ط§ط¹ ط¨ظٹط§ظ†ط§طھ ظ…ط­ط·ط© ط§ظ„طھظˆظ‚ظپ ط¨ظ†ط¬ط§ط­.", dto);
        }

        public async Task<ResponseDto> AddTripScheduleAsync(AddTripScheduleDto dto, int companyId)
        {
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == dto.TripId && t.CompanyId == companyId);

            if (!await _context.Stations.AnyAsync(s => s.StationId == dto.StationId && s.CompanyId == companyId))
                return ResponseDto.FailureResponse("ط§ظ„ظ…ط­ط·ط© ط§ظ„ظ…ط®طھط§ط±ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط© ط£ظˆ ظ„ط§ طھطھط¨ط¹ ظ„ط´ط±ظƒطھظƒظ….");

            var matchingFare = await _context.TripFares
                .FirstOrDefaultAsync(tf => tf.CompanyId == companyId &&
                                         tf.FromGovId == trip!.StartGoveId &&
                                         tf.ToGovId == trip!.EndGoveId &&
                                         tf.StationId == dto.StationId);

            if (matchingFare == null)
                return ResponseDto.FailureResponse("ظ„ط§ طھظˆط¬ط¯ طھط³ط¹ظٹط±ط© ظ…ط¹ط±ظپط© ظ„ظ‡ط°ظ‡ ط§ظ„ظ…ط­ط·ط© ط¹ظ„ظ‰ ظ…ط³ط§ط± ظ‡ط°ظ‡ ط§ظ„ط±ط­ظ„ط©.");

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
                return ResponseDto.SuccessResponse("طھظ…طھ ط¥ط¶ط§ظپط© ظ…ط­ط·ط© ط§ظ„طھظˆظ‚ظپ ظ„ظ„ط±ط­ظ„ط© ط¨ظ†ط¬ط§ط­.");
            }
            catch (FormatException ex)
            {
                return ResponseDto.FailureResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ResponseDto.FailureResponse($"ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹: {ex.Message}");
            }
        }

        public async Task<ResponseDto> UpdateTripScheduleAsync(int scheduleId, UpdateTripScheduleDto dto, int companyId)
        {
            var ts = await _context.TripSchedules
                .Include(ts => ts.Trip)
                .FirstOrDefaultAsync(t => t.TripScheduleId == scheduleId && t.Trip!.CompanyId == companyId);

            if (ts == null) return ResponseDto.FailureResponse("ظ…ط­ط·ط© ط§ظ„طھظˆظ‚ظپ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©.");

            try
            {
                if (dto.DepartureTime.HasValue)
                    ts.DepartureTime = dto.DepartureTime.Value;

                if (dto.SeatFare.HasValue)
                    ts.SeatFare = dto.SeatFare.Value;

                await _context.SaveChangesAsync();
                return ResponseDto.SuccessResponse("طھظ… طھط­ط¯ظٹط« ط¨ظٹط§ظ†ط§طھ ظ…ط­ط·ط© ط§ظ„طھظˆظ‚ظپ ط¨ظ†ط¬ط§ط­.");
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

            if (ts == null) return ResponseDto.FailureResponse("ظ…ط­ط·ط© ط§ظ„طھظˆظ‚ظپ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©.");

            // Check if there are bookings for this schedule before deletion
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.TripScheduleId == scheduleId);
            if (hasBookings)
                return ResponseDto.FailureResponse("ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ظ‡ط°ظ‡ ط§ظ„ظ…ط­ط·ط© ظ„ظˆط¬ظˆط¯ ط­ط¬ظˆط²ط§طھ ظ…ط¤ظƒط¯ط© ظ…ط±طھط¨ط·ط© ط¨ظ‡ط§.");

            _context.TripSchedules.Remove(ts);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("طھظ… ط­ط°ظپ ظ…ط­ط·ط© ط§ظ„طھظˆظ‚ظپ ظ…ظ† ط§ظ„ط±ط­ظ„ط© ط¨ظ†ط¬ط§ط­.");
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

            return Task.FromResult(ResponseDto.SuccessResponse("طھظ… ط§ط³طھط±ط¬ط§ط¹ ط£ظ†ظˆط§ط¹ ط§ظ„ط§ط´طھط±ط§ظƒ ط¨ظ†ط¬ط§ط­.", plans));
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

