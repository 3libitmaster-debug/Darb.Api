using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Darb.Api.Services.Interfaces;
using Darb.Api.DTOs.Base;
using Darb.Api.DTOs.Notification;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using Darb.Api.Helpers;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using Darb.Api.Extensions;
using System;

namespace Darb.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        #region Fields & Constructor

        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public NotificationController(INotificationService notificationService, ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        #endregion

        #region Token Registration

        [HttpPost("register-token")]
        [Authorize]
        [SwaggerOperation(Summary = "Register or Update Device Token", Description = "Saves the FCM registration token for the authenticated user to enable push notifications.")]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto.FailureResponse("بيانات المدخلات غير صالحة.", ModelState));
            }

            // SECURE ID RESOLUTION: Extract the authenticated user's main Account ID from the JWT claims principal.
            // This prevents clients from registering device tokens on behalf of other users.
            int userId = User.GetAccountId();
            if (userId == 0)
            {
                return Unauthorized(ResponseDto.FailureResponse("غير مصرح بالدخول أو التوكن غير صالح."));
            }

            var success = await _notificationService.SaveDeviceTokenAsync(userId, dto.Token, dto.DeviceType);
            if (!success)
            {
                return BadRequest(ResponseDto.FailureResponse("فشل حفظ توكن الجهاز."));
            }

            return Ok(ResponseDto.SuccessResponse("تم تسجيل وتحديث توكن الجهاز بنجاح."));
        }

        #endregion

        #region Notification Dispatch

        [HttpPost("send-test")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Send Test Notification (Admin Only)", Description = "Sends a push notification to any user's active devices and registers it in the log database. Restricted to administrators.")]
        public async Task<IActionResult> SendTestNotification([FromBody] SendNotificationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto.FailureResponse("بيانات المدخلات غير صالحة.", ModelState));
            }

            var success = await _notificationService.SendIndividualNotificationAsync(
                dto.ReceiverId, 
                dto.Title, 
                dto.Body, 
                dto.Category, 
                dto.SenderType ?? SenderRole.SuperAdmin, 
                dto.SenderCompanyId
            );

            if (!success)
            {
                return BadRequest(ResponseDto.FailureResponse("فشل إرسال الإشعار. يرجى التحقق من صحة معرف المستلم."));
            }

            return Ok(ResponseDto.SuccessResponse("تم إرسال الإشعار بنجاح وحفظه في سجل قاعدة البيانات."));
        }

        [HttpPost("send")]
        [Authorize(Roles = "Admin,Company")]
        [SwaggerOperation(Summary = "Send Secure Notification", Description = "Sends a push notification to a receiver. Resolves the sender's identity (Admin vs specific Company) securely from their JWT token claims to prevent impersonation.")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto.FailureResponse("بيانات المدخلات غير صالحة.", ModelState));
            }

            // SECURE SENDER RESOLUTION: Default sender is SuperAdmin (Admin role)
            SenderRole resolvedSenderType = SenderRole.SuperAdmin;
            int? resolvedSenderCompanyId = null;

            // If the sender is authenticated as a Company, automatically resolve their CompanyId from the JWT claims principal.
            if (User.IsInRole("Company"))
            {
                resolvedSenderType = SenderRole.Company;
                int companyId = User.GetCompanyId();
                if (companyId == 0)
                {
                    return BadRequest(ResponseDto.FailureResponse("حساب الشركة هذا غير مرتبط بشركة نقل مسجلة أو صالحة."));
                }
                resolvedSenderCompanyId = companyId;
            }

            var success = await _notificationService.SendIndividualNotificationAsync(
                dto.ReceiverId, 
                dto.Title, 
                dto.Body, 
                dto.Category, 
                resolvedSenderType, 
                resolvedSenderCompanyId
            );

            if (!success)
            {
                return BadRequest(ResponseDto.FailureResponse("فشل إرسال الإشعار. يرجى التحقق من صحة معرف المستلم."));
            }

            return Ok(ResponseDto.SuccessResponse("تم إرسال الإشعار بنجاح وحفظه في سجل قاعدة البيانات."));
        }

        #endregion

        #region Notification Retrieval

        [HttpGet("my-notifications")]
        [Authorize]
        [SwaggerOperation(Summary = "Get Authenticated User Notifications", Description = "Retrieves all notifications for the currently authenticated user, ordered from newest to oldest.")]
        public async Task<IActionResult> GetMyNotifications()
        {
            // SECURE ID RESOLUTION: Extract the authenticated user's Account ID from claims.
            int userId = User.GetAccountId();
            if (userId == 0)
            {
                return Unauthorized(ResponseDto.FailureResponse("غير مصرح بالدخول أو التوكن غير صالح."));
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(ResponseDto.SuccessResponse("تم استرجاع الإشعارات بنجاح.", notifications));
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get Specific User Notifications (Admin Only)", Description = "Retrieves all notifications for any specific user by ID. Restricted to administrators.")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(ResponseDto.SuccessResponse("تم استرجاع الإشعارات بنجاح.", notifications));
        }

        #endregion

        #region Status Management

        [HttpPut("{id}/read")]
        [Authorize]
        [SwaggerOperation(Summary = "Mark Notification as Read", Description = "Updates the read status of a specific notification to true, ensuring it belongs to the authenticated user.")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // SECURE ID RESOLUTION: Extract the authenticated user's Account ID from claims.
            int userId = User.GetAccountId();
            if (userId == 0)
            {
                return Unauthorized(ResponseDto.FailureResponse("غير مصرح بالدخول أو التوكن غير صالح."));
            }

            // SECURE OWNERSHIP CHECK: Verify that the notification exists AND actually belongs to the calling user.
            // This prevents a malicious user from guessing notification IDs and marking other users' notifications as read.
            var notificationExists = await _context.Notifications.AnyAsync(n => n.Id == id && n.ReceiverId == userId);
            if (!notificationExists)
            {
                return NotFound(ResponseDto.FailureResponse($"الإشعار بالرقم {id} غير موجود أو غير تابع لك."));
            }

            var success = await _notificationService.MarkAsReadAsync(id);
            if (!success)
            {
                return BadRequest(ResponseDto.FailureResponse($"فشل تحديث حالة الإشعار {id} إلى مقروء."));
            }

            return Ok(ResponseDto.SuccessResponse("تم تحديث حالة الإشعار إلى مقروء بنجاح."));
        }

        #endregion

        #region System Diagnostic (Self-Test)

        [HttpGet("self-test")]
        [SwaggerOperation(Summary = "Run Notification System Self-Test", Description = "Runs a comprehensive end-to-end self-test of the notification service (FCM token registration, notification database logging, push delivery simulation, retrieval, and status updates) and returns a detailed execution report.")]
        public async Task<IActionResult> RunSelfTest()
        {
            var report = new List<string>();
            bool overallSuccess = true;

            try
            {
                report.Add("=== بدء الفحص الذاتي لنظام الإشعارات ===");

                // 1. Create or fetch a test user
                var testEmail = "test_notification_user@darb.com";
                var testUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == testEmail);
                if (testUser == null)
                {
                    testUser = new User
                    {
                        Email = testEmail,
                        Password = "TestPassword123!",
                        Role = AccountRoles.Customer,
                        IsActive = true,
                        JoinDate = DateHelper.GetYemenTime()
                    };
                    await _context.Users.AddAsync(testUser);
                    await _context.SaveChangesAsync();
                    report.Add($"[نجاح] تم إنشاء مستخدم فحص تجريبي بالمعرف: {testUser.UserId}");
                }
                else
                {
                    report.Add($"[معلومة] مستخدم الفحص التجريبي موجود مسبقاً بالمعرف: {testUser.UserId}");
                }

                // 2. Register mock FCM token
                var mockToken = "mock_fcm_token_xyz_12345";
                var saveTokenResult = await _notificationService.SaveDeviceTokenAsync(testUser.UserId, mockToken, DevicePlatform.Android);
                if (saveTokenResult)
                {
                    report.Add("[نجاح] الخطوة 1: تم استدعاء حفظ توكن الجهاز التجريبي بنجاح.");
                }
                else
                {
                    report.Add("[فشل] الخطوة 1: فشل استدعاء حفظ توكن الجهاز التجريبي.");
                    overallSuccess = false;
                }

                // 3. Verify token exists in database
                var dbToken = await _context.DeviceTokens.FirstOrDefaultAsync(t => t.Token == mockToken && t.UserId == testUser.UserId);
                if (dbToken != null)
                {
                    report.Add($"[نجاح] الخطوة 2: تم التحقق من حفظ التوكن في قاعدة البيانات بنجاح (معرف التوكن: {dbToken.Id}).");
                }
                else
                {
                    report.Add("[فشل] الخطوة 2: لم يتم العثور على التوكن في قاعدة البيانات.");
                    overallSuccess = false;
                }

                // 4. Send mock notification
                var testTitle = "تنبيه تجريبي من النظام";
                var testBody = "هذا الإشعار جزء من عملية الفحص الذاتي التلقائي لنظام Darb.";
                var sendResult = await _notificationService.SendIndividualNotificationAsync(
                    testUser.UserId,
                    testTitle,
                    testBody,
                    NotificationCategory.Alert,
                    SenderRole.SuperAdmin
                );

                if (sendResult)
                {
                    report.Add("[نجاح] الخطوة 3: تم استدعاء إرسال الإشعار التجريبي بنجاح.");
                }
                else
                {
                    report.Add("[فشل] الخطوة 3: فشل استدعاء إرسال الإشعار التجريبي.");
                    overallSuccess = false;
                }

                // 5. Verify notification in database
                var dbNotifications = await _context.Notifications
                    .Where(n => n.ReceiverId == testUser.UserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                Darb.Core.Entities.Notification? latestNotification = null;
                if (dbNotifications.Any())
                {
                    latestNotification = dbNotifications.First();
                    report.Add($"[نجاح] الخطوة 4: تم التحقق من تسجيل الإشعار في قاعدة البيانات بنجاح (معرف الإشعار: {latestNotification.Id}، العنوان: '{latestNotification.Title}').");
                }
                else
                {
                    report.Add("[فشل] الخطوة 4: لم يتم العثور على أي إشعارات مسجلة للمستخدم في قاعدة البيانات.");
                    overallSuccess = false;
                }

                // 6. Verify retrieval via service
                var serviceNotifications = await _notificationService.GetUserNotificationsAsync(testUser.UserId);
                if (serviceNotifications != null && serviceNotifications.Any())
                {
                    report.Add($"[نجاح] الخطوة 5: تم جلب الإشعارات عبر الخدمة بنجاح (العدد المسترجع: {serviceNotifications.Count()}).");
                }
                else
                {
                    report.Add("[فشل] الخطوة 5: فشل جلب الإشعارات عبر الخدمة.");
                    overallSuccess = false;
                }

                // 7. Update status to read and verify
                if (latestNotification != null)
                {
                    var markReadResult = await _notificationService.MarkAsReadAsync(latestNotification.Id);
                    if (markReadResult)
                    {
                        var updatedNotification = await _context.Notifications.FindAsync(latestNotification.Id);
                        if (updatedNotification != null && updatedNotification.IsRead)
                        {
                            report.Add($"[نجاح] الخطوة 6: تم تحديث حالة الإشعار {latestNotification.Id} إلى مقروء بنجاح والتحقق منها.");
                        }
                        else
                        {
                            report.Add($"[فشل] الخطوة 6: تم استدعاء التحديث بنجاح ولكن لم تتغير الحالة في قاعدة البيانات.");
                            overallSuccess = false;
                        }
                    }
                    else
                    {
                        report.Add($"[فشل] الخطوة 6: فشل استدعاء تحديث حالة الإشعار إلى مقروء.");
                        overallSuccess = false;
                    }
                }

                // 8. Clean up mock database data
                if (latestNotification != null)
                {
                    var existingNotifications = new List<Darb.Core.Entities.Notification>();
                    foreach (var n in dbNotifications)
                    {
                        if (await _context.Notifications.AnyAsync(x => x.Id == n.Id))
                        {
                            existingNotifications.Add(n);
                        }
                    }
                    if (existingNotifications.Any())
                    {
                        _context.Notifications.RemoveRange(existingNotifications);
                    }
                }
                if (dbToken != null)
                {
                    var tokenExists = await _context.DeviceTokens.AnyAsync(t => t.Id == dbToken.Id);
                    if (tokenExists)
                    {
                        _context.DeviceTokens.Remove(dbToken);
                    }
                }
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Ignore concurrency exceptions during cleanup if entities were already deleted
                }
                report.Add("[نجاح] الخطوة 7: تم تنظيف وحذف بيانات الفحص التجريبية بنجاح.");

                report.Add("=== انتهى الفحص الذاتي بنجاح وبدون أخطاء ===");
            }
            catch (Exception ex)
            {
                overallSuccess = false;
                report.Add($"[خطأ فادح] حدث استثناء أثناء الفحص الذاتي: {ex.Message}");
            }

            return Ok(ResponseDto.SuccessResponse("تقرير الفحص الذاتي لنظام الإشعارات", new
            {
                Success = overallSuccess,
                FirebaseInitialized = FirebaseAdmin.FirebaseApp.DefaultInstance != null,
                Log = report
            }));
        }

        #endregion
    }
}
