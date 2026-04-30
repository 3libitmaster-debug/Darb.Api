using Darb.Api.Models;

namespace Darb.Api.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(Account Account);
    }
}