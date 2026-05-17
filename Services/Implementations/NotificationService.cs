using Darb.Api.Services.Interfaces;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using Darb.Core.Entities;
using Darb.Api.Helpers;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Darb.Api.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        #region Fields & Constructor

        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #endregion

        #region Device Token Management

        /// <summary>
        /// Saves or updates the FCM device token for a user.
        /// </summary>
        public async Task<bool> SaveDeviceTokenAsync(int userId, string token, DevicePlatform deviceType)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Cannot save an empty or null device token.");
                return false;
            }

            try
            {
                // Verify user exists first to prevent foreign key errors
                var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    _logger.LogWarning($"User with ID {userId} does not exist. Cannot associate device token.");
                    return false;
                }

                // Check if this token is already registered in the system (for any user)
                var existingToken = await _context.DeviceTokens
                    .FirstOrDefaultAsync(d => d.Token == token);

                if (existingToken != null)
                {
                    // If it is registered to another user, update the association and platform
                    existingToken.UserId = userId;
                    existingToken.DeviceType = deviceType;
                    existingToken.LastUpdatedAt = DateHelper.GetYemenTime();
                    _context.DeviceTokens.Update(existingToken);
                    _logger.LogInformation($"Updated existing token association to User ID {userId}.");
                }
                else
                {
                    // Create new device token entry
                    var newDeviceToken = new DeviceToken
                    {
                        UserId = userId,
                        Token = token,
                        DeviceType = deviceType,
                        CreatedAt = DateHelper.GetYemenTime(),
                        LastUpdatedAt = DateHelper.GetYemenTime()
                    };
                    await _context.DeviceTokens.AddAsync(newDeviceToken);
                    _logger.LogInformation($"Registered new device token for User ID {userId}.");
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving device token for User ID {userId}.");
                return false;
            }
        }

        #endregion

        #region Notification Delivery (Database & Firebase FCM)

        /// <summary>
        /// Saves a notification to the database and dispatches Firebase Push Notifications to all registered devices of the user.
        /// </summary>
        public async Task<bool> SendIndividualNotificationAsync(
            int receiverId, 
            string title, 
            string body, 
            NotificationCategory category, 
            SenderRole senderType, 
            int? senderCompanyId = null)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("Notification title or body cannot be empty.");
                return false;
            }

            try
            {
                // 1. Verify user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == receiverId);
                if (!userExists)
                {
                    _logger.LogWarning($"Receiver with ID {receiverId} does not exist. Cannot send notification.");
                    return false;
                }

                // 2. Save notification to local database log
                var notification = new Notification
                {
                    Title = title,
                    Body = body,
                    IsRead = false,
                    CreatedAt = DateHelper.GetYemenTime(),
                    ReceiverId = receiverId,
                    NotificationType = category,
                    SenderType = senderType,
                    SenderCompanyId = senderCompanyId
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Saved notification ID {notification.Id} in database for User ID {receiverId}.");

                // 3. Retrieve all device tokens associated with the receiver
                var deviceTokens = await _context.DeviceTokens
                    .Where(dt => dt.UserId == receiverId)
                    .ToListAsync();

                if (!deviceTokens.Any())
                {
                    _logger.LogInformation($"No device tokens registered for User ID {receiverId}. Notification stored in database only.");
                    return true; 
                }

                // 4. Send Firebase Push Notification if Firebase is initialized
                if (FirebaseApp.DefaultInstance != null)
                {
                    _logger.LogInformation($"Sending push notifications to {deviceTokens.Count} device(s) for User ID {receiverId}...");

                    // PERFORMANCE OPTIMIZATION: Fetch the company sender details ONCE outside the device loop
                    // to prevent executing N database queries for N device tokens.
                    string? senderCompanyName = null;
                    string? senderCompanyLogo = null;
                    if (senderCompanyId.HasValue && senderType == SenderRole.Company)
                    {
                        var company = await _context.Companies.FindAsync(senderCompanyId.Value);
                        if (company != null)
                        {
                            senderCompanyName = company.Name;
                            senderCompanyLogo = company.Logo;
                        }
                    }

                    foreach (var dt in deviceTokens)
                    {
                        try
                        {
                            // Build a rich data payload. This enables the Flutter client to instantly know
                            // who sent the notification (Admin or a specific Company with its name and logo)
                            // even when the app is in the background or terminated.
                            var dataPayload = new Dictionary<string, string>()
                            {
                                { "notificationId", notification.Id.ToString() },
                                { "category", category.ToString() },
                                { "senderType", senderType.ToString() },
                                { "click_action", "FLUTTER_NOTIFICATION_CLICK" }
                            };

                            if (senderCompanyId.HasValue)
                            {
                                dataPayload.Add("senderCompanyId", senderCompanyId.Value.ToString());
                                if (!string.IsNullOrEmpty(senderCompanyName))
                                {
                                    dataPayload.Add("senderCompanyName", senderCompanyName);
                                }
                                if (!string.IsNullOrEmpty(senderCompanyLogo))
                                {
                                    dataPayload.Add("senderCompanyLogo", senderCompanyLogo);
                                }
                            }

                            var message = new FirebaseAdmin.Messaging.Message()
                            {
                                Token = dt.Token,
                                Notification = new FirebaseAdmin.Messaging.Notification()
                                {
                                    Title = title,
                                    Body = body
                                },
                                Data = dataPayload
                            };

                            string response = await FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance.SendAsync(message);
                            _logger.LogInformation($"Successfully sent FCM push notification. Message ID: {response}");
                        }
                        catch (FirebaseAdmin.Messaging.FirebaseMessagingException ex) when (
                            ex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.Unregistered || 
                            ex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.InvalidArgument)
                        {
                            // Stale or invalid token. Clean it up from database
                            _logger.LogWarning($"FCM token for user {receiverId} is unregistered or invalid. Removing from database: {dt.Token}");
                            _context.DeviceTokens.Remove(dt);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send FCM notification to token {dt.Token} for user {receiverId}.");
                        }
                    }

                    // Save any changes if some tokens were removed
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning($"Firebase Admin SDK is not initialized. Skipping push notification dispatch for User ID {receiverId}. Notification remains saved in DB.");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling notification dispatch for User ID {receiverId}.");
                return false;
            }
        }

        #endregion

        #region Notification Retrieval

        /// <summary>
        /// Retrieves notifications for a specific user, ordered from newest to oldest.
        /// Eagerly loads the associated Sender Company details if the sender is a Company.
        /// </summary>
        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId)
        {
            try
            {
                // We use eager loading via .Include(n => n.SenderCompany) so that when the Flutter client
                // requests their notifications list, the JSON serializer will automatically serialize
                // the sender company details (ID, Name, Logo URL). This avoids manual mapping or multiple API calls.
                return await _context.Notifications
                    .Include(n => n.SenderCompany)
                    .Where(n => n.ReceiverId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving notifications for User ID {userId}.");
                return Enumerable.Empty<Notification>();
            }
        }

        #endregion

        #region Read Status Management

        /// <summary>
        /// Marks a notification as read in the database.
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null)
                {
                    _logger.LogWarning($"Notification with ID {notificationId} not found.");
                    return false;
                }

                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Notification ID {notificationId} marked as read.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {notificationId} as read.");
                return false;
            }
        }

        #endregion
    }
}
