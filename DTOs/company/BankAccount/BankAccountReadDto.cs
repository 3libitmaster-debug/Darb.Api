namespace Darb.Api.DTOs.BankAccount
{
    public class BankAccountReadDto
    {
        public int BankAccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public int BankId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }
}
