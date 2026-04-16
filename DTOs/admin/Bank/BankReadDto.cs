namespace Darb.Api.DTOs.adminDtos.Bank
{
    public class BankReadDto
    {
        public int BankId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }
}
