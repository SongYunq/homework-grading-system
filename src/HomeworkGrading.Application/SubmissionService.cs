using HomeworkGrading.Domain;

namespace HomeworkGrading.Application;

public sealed class SubmissionService(IHomeworkStore homework, IGradingAdvisor gradingAdvisor, TimeProvider clock)
{
    public async Task<SubmissionDetail> SubmitAsync(Actor actor, Guid assignmentId, SubmitRequest request, CancellationToken ct)
    {
        HomeworkAccess.RequireRole(actor, UserRole.Student);
        var submission = await homework.ExecuteWriteAsync(async token =>
        {
            var assignment = await HomeworkAccess.FindVisibleAssignmentAsync(homework, actor, assignmentId, token);
            var alreadySubmitted = await homework.FindStudentSubmissionAsync(assignmentId, actor.Id, token) is not null;
            var entity = UseCaseException.ApplyDomain(() =>
                assignment.AcceptSubmission(actor.Id, request.Content ?? "", alreadySubmitted, clock.GetUtcNow()));
            homework.Add(entity);
            return entity;
        }, ct);

        if (!string.IsNullOrWhiteSpace(actor.Name))
            return SubmissionDetail.From(submission, actor.Name);

        // Authenticated HTTP callers supply the name claim. Other adapters may load it with the submission.
        var saved = await homework.FindSubmissionAsync(submission.Id, ct);
        return SubmissionDetail.From(saved ?? throw new InvalidOperationException("Saved submission could not be loaded."));
    }

    public async Task<SubmissionDetail> GetAsync(Actor actor, Guid id, CancellationToken ct)
    {
        HomeworkAccess.ValidateActor(actor);
        var record = await FindVisibleSubmissionAsync(actor, id, ct);
        return SubmissionDetail.From(record);
    }

    public Task<SubmissionDetail> GradeAsync(Actor actor, Guid id, GradeRequest request, CancellationToken ct)
    {
        HomeworkAccess.RequireRole(actor, UserRole.Teacher);
        return homework.ExecuteWriteAsync(async token =>
        {
            var (record, assignment) = await FindTeacherSubmissionAsync(actor, id, token);
            if (request.Score is null)
                throw UseCaseException.Validation("invalid_score", "请填写分数。");

            UseCaseException.ApplyDomain(() =>
                record.Submission.Grade(request.Score.Value, request.Feedback ?? "", assignment.MaxScore, clock.GetUtcNow()));
            return SubmissionDetail.From(record);
        }, ct);
    }

    public async Task<GradingSuggestion> SuggestAsync(Actor actor, Guid id, CancellationToken ct)
    {
        HomeworkAccess.RequireRole(actor, UserRole.Teacher);
        var (record, assignment) = await FindTeacherSubmissionAsync(actor, id, ct);
        if (assignment.Rubric is null)
            throw UseCaseException.Conflict("grading_rubric_required", "这份作业发布时未设置评分标准，请使用人工批改。");

        var input = new GradingInput(assignment.Subject, assignment.Description, assignment.ReferenceAnswer,
            assignment.Rubric, record.Submission.Content, assignment.MaxScore);
        var suggestion = await gradingAdvisor.SuggestAsync(input, ct);
        if (!Submission.IsValidScore(suggestion.Score, assignment.MaxScore) ||
            string.IsNullOrWhiteSpace(suggestion.Feedback) || suggestion.Feedback.Trim().Length > 5_000 ||
            string.IsNullOrWhiteSpace(suggestion.Model))
        {
            throw new UseCaseException(UseCaseError.BadGateway, "invalid_grading_suggestion", "批改建议未通过格式校验。");
        }

        return suggestion with { Feedback = suggestion.Feedback.Trim() };
    }

    private async Task<SubmissionRecord> FindVisibleSubmissionAsync(Actor actor, Guid id, CancellationToken ct)
    {
        if (actor.Role == UserRole.Teacher)
            return (await FindTeacherSubmissionAsync(actor, id, ct)).Record;

        var record = await homework.FindSubmissionAsync(id, ct);
        if (record is null || record.Submission.StudentId != actor.Id)
            throw NotFound();

        return record;
    }

    private async Task<(SubmissionRecord Record, Assignment Assignment)> FindTeacherSubmissionAsync(
        Actor actor, Guid id, CancellationToken ct)
    {
        var record = await homework.FindSubmissionAsync(id, ct);
        if (record is null)
            throw NotFound();

        var assignment = await homework.FindAssignmentAsync(record.Submission.AssignmentId, ct);
        if (assignment is null || assignment.TeacherId != actor.Id)
            throw NotFound();

        return (record, assignment);
    }

    private static UseCaseException NotFound() =>
        UseCaseException.NotFound("submission_not_found", "找不到这份提交。");
}
