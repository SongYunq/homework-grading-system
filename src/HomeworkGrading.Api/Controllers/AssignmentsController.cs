using HomeworkGrading.Api.Http;
using HomeworkGrading.Application;
using HomeworkGrading.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeworkGrading.Api.Controllers;

[ApiController]
[Route("api/v1/assignments")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class AssignmentsController(AssignmentService assignments, SubmissionService submissions) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AssignmentList>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentList>> List(CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string status = "all")
    {
        AssignmentStatus? filter = status.ToLowerInvariant() switch
        {
            "all" => null,
            "open" => AssignmentStatus.Open,
            "closed" => AssignmentStatus.Closed,
            "pending" => AssignmentStatus.Pending,
            "graded" => AssignmentStatus.Graded,
            _ => throw new UseCaseException(UseCaseError.Validation, "invalid_status", "status 必须为 all、open、closed、pending 或 graded。")
        };
        return Ok(await assignments.ListAsync(User.Actor(), new AssignmentListQuery(page, pageSize, filter), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AssignmentDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDetail>> Get(Guid id, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await assignments.GetAsync(User.Actor(), id, new PageQuery(page, pageSize), cancellationToken));

    [Authorize(Roles = nameof(UserRole.Teacher))]
    [HttpPost]
    [ProducesResponseType<AssignmentDetail>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssignmentDetail>> Create(AssignmentRequest request, CancellationToken cancellationToken)
    {
        var result = await assignments.CreateAsync(User.Actor(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [Authorize(Roles = nameof(UserRole.Teacher))]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<AssignmentDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignmentDetail>> Update(Guid id, AssignmentRequest request, CancellationToken cancellationToken) =>
        Ok(await assignments.UpdateAsync(User.Actor(), id, request, cancellationToken));

    [Authorize(Roles = nameof(UserRole.Teacher))]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await assignments.DeleteAsync(User.Actor(), id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Student))]
    [HttpPost("{id:guid}/submissions")]
    [ProducesResponseType<SubmissionDetail>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionDetail>> Submit(Guid id, SubmitRequest request, CancellationToken cancellationToken)
    {
        var result = await submissions.SubmitAsync(User.Actor(), id, request, cancellationToken);
        return CreatedAtRoute("GetSubmission", new { id = result.Id }, result);
    }
}
