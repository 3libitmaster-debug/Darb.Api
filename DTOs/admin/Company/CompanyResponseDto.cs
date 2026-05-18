namespace Darb.Api.DTOs.admin.Company
{
    public class CompanyResponseDto
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string License { get; set; } = null!;
        public string? LogoUrl { get; set; } // الرابط الكامل للصورة لعرضه بـ Image.network
        public double AverageRating { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinDate { get; set; }
    }
}
