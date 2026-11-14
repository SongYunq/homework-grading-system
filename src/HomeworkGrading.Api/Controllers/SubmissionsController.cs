using HomeworkGrading.Api.Http;
using HomeworkGrading.Application;
using HomeworkGrading.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeworkGrading.Api.Controllers;

[ApiController]
[Route("api/v1/submissions")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public sealed class SubmissionsController(SubmissionService submissions) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetSubmission")]
    [ProducesResponseType<SubmissionDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubmissionDetail>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await submissions.GetAsync(User.Actor(), id, cancellationToken));

    [Authorize(Roles = nameof(UserRole.Teacher))]
    [HttpPut("{id:guid}/grade")]
    [ProducesResponseType<SubmissionDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SubmissionDetail>> Grade(Guid id, GradeRequest request, CancellationToken cancellationToken) =>
        Ok(await submissions.GradeAsync(User.Actor(), id, request, cancellationToken));

    [Authorize(Roles = nameof(UserRole.Teacher))]
    [EnableRateLimiting("suggestion")]
    [HttpPost("{id:guid}/grade-suggestions")]
    [ProducesResponseType<GradingSuggestion>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<GradingSuggestion>> Suggest(Guid id, CancellationToken cancellationToken) =>
        Ok(await submissions.SuggestAsync(User.Actor(), id, cancellationToken));
}
