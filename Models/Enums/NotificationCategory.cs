namespace Darb.Api.Models.Enums
{
    public enum NotificationCategory
    {
        Transaction = 1, // عمليات (تأكيد حجز، إلغاء حجز، دفع)
        Update = 2,      // تحديثات (تعديل موعد رحلة، تغيير حافلة)
        Alert = 3,       // تنبيهات وتحذيرات (مخالفة شركة، إلغاء رحلة طارئ)
        Reminder = 4     // تذكيرات (تذكير بالرحلة قبل الانطلاق بساعتين)
    }
}
