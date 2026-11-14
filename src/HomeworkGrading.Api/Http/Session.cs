using System.Security.Claims;
using HomeworkGrading.Application;
using HomeworkGrading.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HomeworkGrading.Api.Http;

internal static class Session
{
    public const string WriteHeader = "X-Homework-Request";

    public static Task SignInAsync(HttpContext context, AccountDto account)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Name),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, account.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    public static Actor Actor(this ClaimsPrincipal principal) => new(
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Enum.Parse<UserRole>(principal.FindFirstValue(ClaimTypes.Role)!),
        principal.Identity?.Name);

    public static AccountDto Account(this ClaimsPrincipal principal)
    {
        var actor = principal.Actor();
        return new AccountDto(actor.Id, principal.Identity!.Name!, principal.FindFirstValue(ClaimTypes.Email)!, actor.Role);
    }
}
