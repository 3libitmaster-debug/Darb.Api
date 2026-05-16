using System.Security.Claims;

namespace Darb.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetCompanyId(this ClaimsPrincipal User)
    {

        var claim = User.FindFirst("CompanyId")?.Value;

        return int.TryParse(claim, out int id) ? id : 0;
    }

    public static int GetAccountId(this ClaimsPrincipal User)
    {
       
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    public static int GetPassengerId(this ClaimsPrincipal User)
    {

        var claim = User.FindFirst("CustomerId")?.Value;

        return int.TryParse(claim, out int id) ? id : 0;
    }
}