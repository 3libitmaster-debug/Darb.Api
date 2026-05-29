namespace Darb.Api.DTOs.admin
{
    public class DashboardStatsDto
    {
        public int NewRegistrationsCount { get; set; }  // عدد طلبات الاشتراك الجديدة
        public int RenewalRequestsCount { get; set; }   // عدد طلبات تجديد الاشتراك
        public int TotalCustomersCount { get; set; }    // عدد العملاء
        public int ActiveAdsCount { get; set; }         // عدد الإعلانات النشطة
    }
}
