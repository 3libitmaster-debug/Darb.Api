using System.Security.Claims;

namespace Darb.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetCompanyId(this ClaimsPrincipal Account)
    {

        var claim = Account.FindFirst("CompanyId")?.Value;

        return int.TryParse(claim, out int id) ? id : 0;
    }

    public static int GetAccountId(this ClaimsPrincipal Account)
    {
       
        var claim = Account.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    public static int GetPassengerId(this ClaimsPrincipal Account)
    {

        var claim = Account.FindFirst("PassengerId")?.Value;

        return int.TryParse(claim, out int id) ? id : 0;
    }
}