using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HomeworkGrading.Api.Http;

internal sealed class ApiOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var api = context.ApiDescription.RelativePath?.StartsWith("api/", StringComparison.Ordinal) == true;
        if (!api)
            return;

        if (context.ApiDescription.HttpMethod is "POST" or "PUT" or "DELETE" or "PATCH")
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                In = ParameterLocation.Header,
                Name = Session.WriteHeader,
                Required = true,
                Description = "固定值 1。浏览器须通过同源地址调用。",
                Schema = new OpenApiSchema { Type = "string", Default = new Microsoft.OpenApi.Any.OpenApiString("1") }
            });
            AddProblem("400", "Bad Request");
        }

        if (operation.RequestBody is not null)
        {
            AddProblem("413", "Content Too Large");
            AddProblem("415", "Unsupported Media Type");
        }

        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<EnableRateLimitingAttribute>().Any())
            AddProblem("429", "Too Many Requests");
        AddProblem("500", "Internal Server Error");

        foreach (var (code, response) in operation.Responses)
        {
            if (code.Length != 3 || code[0] is not ('4' or '5'))
                continue;
            response.Content.Clear();
            response.Content["application/problem+json"] = new OpenApiMediaType
            {
                Schema = context.SchemaGenerator.GenerateSchema(typeof(ApiProblemSchema), context.SchemaRepository)
            };
        }

        void AddProblem(string code, string description) =>
            operation.Responses.TryAdd(code, new OpenApiResponse { Description = description });
    }

    // ProblemDetails serializes the extension dictionary as top-level fields.
    private sealed record ApiProblemSchema(
        string Type, string Title, int Status, string? Detail, string Instance,
        string Code, string TraceId, Dictionary<string, string[]>? Errors);
}
