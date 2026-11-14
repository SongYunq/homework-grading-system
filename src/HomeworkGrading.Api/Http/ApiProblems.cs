using System.Diagnostics;
using HomeworkGrading.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace HomeworkGrading.Api.Http;

internal static class ApiProblems
{
    public static ProblemDetails Create(HttpContext context, int status, string? code = null, string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = ReasonPhrases.GetReasonPhrase(status),
            Status = status,
            Instance = $"{context.Request.PathBase}{context.Request.Path}",
            Detail = detail
        };
        problem.Extensions["code"] = code ?? status switch
        {
            400 => "invalid_request",
            401 => "authentication_required",
            403 => "forbidden",
            404 => "not_found",
            405 => "method_not_allowed",
            413 => "request_too_large",
            415 => "unsupported_media_type",
            429 => "too_many_requests",
            500 => "internal_error",
            _ => $"http_{status}"
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        return problem;
    }

    public static Task WriteAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status!.Value;
        if (problem.Status == StatusCodes.Status401Unauthorized)
            context.Response.Headers.WWWAuthenticate = "Session realm=\"homework\"";
        return context.Response.WriteAsJsonAsync(problem, options: null,
            contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }

    public static async Task HandleUseCaseErrorsAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (UseCaseException error) when (!context.Response.HasStarted)
        {
            await WriteAsync(context, Create(context, Map(error.Status), error.Code, error.Message));
        }
    }

    private static int Map(UseCaseError error) => error switch
    {
        UseCaseError.Validation => StatusCodes.Status400BadRequest,
        UseCaseError.Unauthorized => StatusCodes.Status401Unauthorized,
        UseCaseError.Forbidden => StatusCodes.Status403Forbidden,
        UseCaseError.NotFound => StatusCodes.Status404NotFound,
        UseCaseError.Conflict => StatusCodes.Status409Conflict,
        UseCaseError.Unavailable => StatusCodes.Status503ServiceUnavailable,
        UseCaseError.BadGateway => StatusCodes.Status502BadGateway,
        UseCaseError.GatewayTimeout => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError
    };
}

internal sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            BadHttpRequestException error => ApiProblems.Create(context, error.StatusCode),
            _ => ApiProblems.Create(context, StatusCodes.Status500InternalServerError)
        };
        await ApiProblems.WriteAsync(context, problem);
        return true;
    }

}
