using HomeworkGrading.Api.Http;
using HomeworkGrading.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeworkGrading.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AccountService accounts) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("register")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountDto>> Register(RegisterStudentRequest request, CancellationToken cancellationToken)
    {
        var account = await accounts.RegisterStudentAsync(request, cancellationToken);
        await Session.SignInAsync(HttpContext, account);
        return CreatedAtAction(nameof(Me), account);
    }

    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("login")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccountDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var account = await accounts.LoginAsync(request, cancellationToken);
        await Session.SignInAsync(HttpContext, account);
        return Ok(account);
    }

    [HttpGet("me")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<AccountDto> Me() => Ok(User.Account());

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }
}
