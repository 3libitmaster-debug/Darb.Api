namespace Darb.Api.DTOs.company.Booking
{
    public class CompanyDetailedPassengerDto
    {
        public int PassengerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = "غير متوفر";
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? BirthDate { get; set; }
    }
}
