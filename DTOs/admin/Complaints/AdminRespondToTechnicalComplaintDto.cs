namespace Darb.Api.DTOs.admin.Complaints
{
    /// <summary>
    /// DTO لرد الأدمن على شكوى دعم فني.
    /// لا يحتاج إلى بيانات إضافية — الاستجابة تلقائية بالكامل.
    /// </summary>
    public class AdminRespondToTechnicalComplaintDto
    {
        // لا توجد حقول مطلوبة من الأدمن:
        // الحالة تُحوَّل تلقائياً إلى Resolved والإشعار يُرسل تلقائياً.
    }
}
