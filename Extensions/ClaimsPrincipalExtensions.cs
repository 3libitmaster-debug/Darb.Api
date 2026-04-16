using System.Security.Claims;

namespace Darb.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetCompanyId(this ClaimsPrincipal user)
    {

        var claim = user.FindFirst("CompanyId")?.Value;

        return int.TryParse(claim, out int id) ? id : 0;
    }

    public static int GetUserId(this ClaimsPrincipal user)
    {
       
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    public static int GetPassengerId(this ClaimsPrincipal user)
    {

        var claim = user.FindFirst("PassengerId")?.Value;

        return int.TryParse(claim, out int id) ? id : 0;
    }
}