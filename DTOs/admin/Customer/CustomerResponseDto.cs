namespace Darb.Api.DTOs.admin.Customers
{
    public class CustomerResponseDto
    {
        public int UserId { get; set; }
        public int CustomerId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string NationalId { get; set; } = null!;
        public DateTime JoinDate { get; set; }
        public bool IsActive { get; set; }
    }
}
