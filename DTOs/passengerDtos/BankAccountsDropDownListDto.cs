namespace Darb.Api.DTOs.BankAccount
{
    public class BankAccountsDropDownListDto
    {
        public int BankAccountId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }
}
