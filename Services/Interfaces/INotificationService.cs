using Darb.Api.Models.Enums;
using Darb.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darb.Api.Services.Interfaces
{
    public interface INotificationService
    {
        // لحفظ وتحديث توكن الجهاز القادم من الفلاتر
        Task<bool> SaveDeviceTokenAsync(int userId, string token, DevicePlatform deviceType);

        // لإرسال إشعار للمستخدم وحفظه في السجل
        Task<bool> SendIndividualNotificationAsync(int receiverId, string title, string body, NotificationCategory category, SenderRole senderType, int? senderCompanyId = null);

        // لجلب إشعارات مستخدم معين مرتبة من الأحدث إلى الأقدم
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);

        // لتحديث حالة الإشعار إلى مقروء
        Task<bool> MarkAsReadAsync(int notificationId);
    }
}
