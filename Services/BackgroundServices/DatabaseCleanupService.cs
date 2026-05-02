using Darb.Api.Helpers;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darb.Api.Services.BackgroundServices
{
    public class DatabaseCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseCleanupService> _logger;

        public DatabaseCleanupService(IServiceProvider serviceProvider, ILogger<DatabaseCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DatabaseCleanupService is starting.");

            // Run periodically
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupPendingBookingsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing DatabaseCleanupService.");
                }

                // Wait 5 minutes before checking again
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task CleanupPendingBookingsAsync(CancellationToken cancellationToken)
        {
            // Create a scope to resolve scoped services like ApplicationDbContext
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Calculate the time threshold (30 minutes ago)
            var timeThreshold = DateHelper.GetYemenTime().AddMinutes(-30);

            // Find matching bookings
            var expiredBookings = await dbContext.Bookings
                .Include(b => b.TripSchedule)
                    .ThenInclude(tr => tr!.Trip)
                .Where(b => b.Status == BookingStatus.PendingAttachment  ||
                            b.ReceiptImagePath == null && 
                            b.BookingAt <= timeThreshold)
                .ToListAsync(cancellationToken);

            if (expiredBookings.Any())
            {
                _logger.LogInformation($"Found {expiredBookings.Count} expired pending bookings to clean up.");

                foreach (var booking in expiredBookings)
                {
                    if (booking.TripSchedule?.Trip != null)
                    {
                        // Restore trip seats
                        booking.TripSchedule.Trip.AvailableSeats += booking.ReservedSeatsCount;
                    }
                }

                // Delete bookings (Cascade delete will handle dependent entities like ETickets, PassengerDetails)
                dbContext.Bookings.RemoveRange(expiredBookings);
                
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"Successfully cleaned up {expiredBookings.Count} bookings and restored trip seats.");
            }
        }
    }
}
