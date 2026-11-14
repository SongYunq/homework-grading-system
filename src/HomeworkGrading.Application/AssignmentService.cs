using HomeworkGrading.Domain;

namespace HomeworkGrading.Application;

public sealed class AssignmentService(IHomeworkStore homework, TimeProvider clock)
{
    public Task<AssignmentList> ListAsync(Actor actor, AssignmentListQuery query, CancellationToken ct)
    {
        HomeworkAccess.ValidateActor(actor);
        ValidatePage(query.Page, query.PageSize);
        if (query.Status is { } status && !Enum.IsDefined(status))
            throw UseCaseException.Validation("invalid_status", "不支持该作业状态。");

        return homework.ListAsync(actor, query, clock.GetUtcNow(), ct);
    }

    public async Task<AssignmentDetail> GetAsync(Actor actor, Guid id, PageQuery page, CancellationToken ct)
    {
        HomeworkAccess.ValidateActor(actor);
        ValidatePage(page.Page, page.PageSize);
        var assignment = await HomeworkAccess.FindVisibleAssignmentAsync(homework, actor, id, ct);
        if (actor.Role == UserRole.Teacher)
        {
            var submissions = await homework.ListSubmissionsAsync(id, page, ct);
            return ToDetail(assignment, teacher: true, submissions);
        }

        var mine = await homework.FindStudentSubmissionAsync(id, actor.Id, ct);
        return ToDetail(assignment, teacher: false,
            mySubmission: mine is null ? null : SubmissionDetail.From(mine));
    }

    public Task<AssignmentDetail> CreateAsync(Actor actor, AssignmentRequest request, CancellationToken ct)
    {
        HomeworkAccess.RequireRole(actor, UserRole.Teacher);
        var draft = ToDraft(request);
        return homework.ExecuteWriteAsync(_ =>
        {
            var assignment = UseCaseException.ApplyDomain(() => new Assignment(actor.Id, draft, clock.GetUtcNow()));
            homework.Add(assignment);
            return Task.FromResult(ToDetail(assignment, teacher: true, EmptySubmissions()));
        }, ct);
    }

    public Task<AssignmentDetail> UpdateAsync(Actor actor, Guid id, AssignmentRequest request, CancellationToken ct)
    {
        HomeworkAccess.RequireRole(actor, UserRole.Teacher);
        return homework.ExecuteWriteAsync(async token =>
        {
            var assignment = await HomeworkAccess.FindVisibleAssignmentAsync(homework, actor, id, token);
            var hasSubmissions = await homework.HasSubmissionsAsync(id, token);
            var draft = ToDraft(request);
            UseCaseException.ApplyDomain(() => assignment.Revise(draft, hasSubmissions, clock.GetUtcNow()));
            return ToDetail(assignment, teacher: true, EmptySubmissions());
        }, ct);
    }

    public Task DeleteAsync(Actor actor, Guid id, CancellationToken ct)
    {
        HomeworkAccess.RequireRole(actor, UserRole.Teacher);
        return homework.ExecuteWriteAsync(async token =>
        {
            var assignment = await HomeworkAccess.FindVisibleAssignmentAsync(homework, actor, id, token);
            var hasSubmissions = await homework.HasSubmissionsAsync(id, token);
            UseCaseException.ApplyDomain(() => assignment.EnsureCanDelete(hasSubmissions));
            homework.Remove(assignment);
            return true;
        }, ct);
    }

    private static AssignmentDraft ToDraft(AssignmentRequest request)
    {
        if (request.MaxScore is null)
            throw UseCaseException.Validation("invalid_max_score", "请填写作业满分。");
        if (request.DueAt is null)
            throw UseCaseException.Validation("invalid_due_at", "请填写截止时间。");

        return new AssignmentDraft(request.Title ?? "", request.Subject ?? "", request.Description ?? "",
            request.ReferenceAnswer, request.Rubric, request.MaxScore.Value, request.DueAt.Value);
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || ((long)page - 1) * pageSize > int.MaxValue)
            throw UseCaseException.Validation("invalid_pagination", "页码必须大于 0，每页须为 1 至 100 条，且分页偏移不能超出范围。");
    }

    private static SubmissionPage EmptySubmissions() => new(Array.Empty<SubmissionSummary>(), 1, 20, 0);

    private static AssignmentDetail ToDetail(Assignment a, bool teacher,
        SubmissionPage? submissions = null, SubmissionDetail? mySubmission = null) =>
        new(a.Id, a.Title, a.Subject, a.Description, a.MaxScore, a.DueAt, a.CreatedAt,
            teacher ? a.ReferenceAnswer : null, teacher ? a.Rubric : null, submissions, mySubmission);
}

internal static class HomeworkAccess
{
    public static void ValidateActor(Actor actor)
    {
        if (actor.Id == Guid.Empty || !Enum.IsDefined(actor.Role))
            throw new UseCaseException(UseCaseError.Unauthorized, "invalid_session", "请重新登录。");
    }

    public static void RequireRole(Actor actor, UserRole role)
    {
        ValidateActor(actor);
        if (actor.Role != role)
            throw UseCaseException.Forbidden("insufficient_permissions", "当前账号没有该操作的权限。");
    }

    public static async Task<Assignment> FindVisibleAssignmentAsync(
        IHomeworkStore homework, Actor actor, Guid id, CancellationToken ct)
    {
        var assignment = await homework.FindAssignmentAsync(id, ct);
        if (assignment is null || (actor.Role == UserRole.Teacher && assignment.TeacherId != actor.Id))
            throw UseCaseException.NotFound("assignment_not_found", "找不到这份作业。");

        return assignment;
    }
}
