using Microsoft.AspNetCore.Http;

namespace TokenProvider.Infrastructure.Services;

public static class CookieGeneratorService
{
    public static CookieOptions GenerateCookie(DateTimeOffset expiryDate)
    {
        var cookkieOption = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = expiryDate,
        };
        return cookkieOption;
    }
}
