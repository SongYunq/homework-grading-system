using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using HomeworkGrading.Api.Http;
using HomeworkGrading.Application;
using HomeworkGrading.Infrastructure;
using HomeworkGrading.Infrastructure.Grading;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var development = builder.Environment.IsDevelopment();
var dataDirectory = Path.GetFullPath(builder.Configuration["Storage:DataDirectory"] ?? "App_Data",
    builder.Environment.ContentRootPath);
Directory.CreateDirectory(dataDirectory);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 256 * 1024);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<SubmissionService>();
var connectionString = builder.Configuration.GetConnectionString("Homework") ??
    new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = Path.Combine(dataDirectory, "homework.db") }.ToString();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddHttpClient<IGradingAdvisor, ChatCompletionAdvisor>(http =>
{
    var seconds = Math.Clamp(builder.Configuration.GetValue("Grading:TimeoutSeconds", 40), 1, 120);
    http.Timeout = TimeSpan.FromSeconds(seconds);
    http.MaxResponseContentBufferSize = 128 * 1024;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Services.AddDataProtection().SetApplicationName("HomeworkGrading")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = development ? "HomeworkGrading.Session" : "__Host-HomeworkGrading.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("suggestion", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    options.JsonSerializerOptions.Converters.Add(new OffsetDateTimeConverter());
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
}).ConfigureApiBehaviorOptions(options =>
{
    options.SuppressMapClientErrors = true;
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = ApiProblems.Create(context.HttpContext, StatusCodes.Status400BadRequest, "invalid_request", "请检查请求字段及其类型。");
        problem.Extensions["errors"] = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
            .GroupBy(entry => string.IsNullOrEmpty(entry.Key) ? "request" : entry.Key)
            .ToDictionary(group => group.Key,
                _ => new[] { "字段缺失或格式不正确。" });
        return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { "application/problem+json" } };
    };
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Homework Grading API", Version = "v1" });
    options.SupportNonNullableReferenceTypes();
    options.OperationFilter<ApiOperationFilter>();
});
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(context => ApiProblems.WriteAsync(context.HttpContext,
    ApiProblems.Create(context.HttpContext, context.HttpContext.Response.StatusCode)));
app.Use(ApiProblems.HandleUseCaseErrorsAsync);
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    if (context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
if (development)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
// Same-origin API writes require a header that browser forms cannot send. No CORS policy allows preflight.
app.Use(async (context, next) =>
{
    if (context.GetEndpoint() is not null && context.Request.Path.StartsWithSegments("/api") &&
        context.Request.Method is "POST" or "PUT" or "DELETE" or "PATCH" &&
        !string.Equals(context.Request.Headers[Session.WriteHeader].ToString(), "1", StringComparison.Ordinal))
    {
        await ApiProblems.WriteAsync(context, ApiProblems.Create(context, StatusCodes.Status400BadRequest,
            "missing_write_header", $"写请求需要 {Session.WriteHeader}: 1 请求头。"));
        return;
    }
    await next(context);
});

app.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok"))).AllowAnonymous();
app.MapControllers().RequireAuthorization();

await app.Services.InitializeInfrastructureAsync(
    development && builder.Configuration.GetValue<bool>("DemoData:Enabled"),
    builder.Configuration["Bootstrap:TeacherEmail"], builder.Configuration["Bootstrap:TeacherPassword"],
    app.Lifetime.ApplicationStopping);
await app.RunAsync();

internal sealed record HealthResponse(string Status);
