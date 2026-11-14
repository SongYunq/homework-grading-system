using System.Text.Json.Serialization;
using HomeworkGrading.Domain;

namespace HomeworkGrading.Application;

public sealed record Actor(Guid Id, UserRole Role, string? Name = null);

public enum AssignmentStatus
{
    Open,
    Closed,
    Pending,
    Graded
}

public sealed record AssignmentListQuery(int Page = 1, int PageSize = 20, AssignmentStatus? Status = null);
public sealed record PageQuery(int Page = 1, int PageSize = 20);

public sealed record AssignmentListItem(
    Guid Id, string Title, string Subject, decimal MaxScore,
    DateTimeOffset DueAt, DateTimeOffset CreatedAt, AssignmentStatus Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? SubmissionCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? GradedCount);

public sealed record AssignmentSummary(int Open, int Closed, int Pending, int Graded);

public sealed record AssignmentList(
    IReadOnlyList<AssignmentListItem> Items, int Page, int PageSize, int TotalCount, AssignmentSummary Summary);

public sealed record AssignmentRequest(
    string? Title, string? Subject, string? Description, string? ReferenceAnswer,
    string? Rubric, decimal? MaxScore, DateTimeOffset? DueAt);

public sealed record AssignmentDetail(
    Guid Id, string Title, string Subject, string Description, decimal MaxScore,
    DateTimeOffset DueAt, DateTimeOffset CreatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReferenceAnswer,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Rubric,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] SubmissionPage? Submissions,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] SubmissionDetail? MySubmission);

public sealed record SubmitRequest(string? Content);
public sealed record GradeRequest(decimal? Score, string? Feedback);

public sealed record SubmissionSummary(
    Guid Id, Guid StudentId, string StudentName, DateTimeOffset SubmittedAt, decimal? Score, DateTimeOffset? GradedAt);

public sealed record SubmissionPage(IReadOnlyList<SubmissionSummary> Items, int Page, int PageSize, int TotalCount);

public sealed record SubmissionDetail(
    Guid Id, Guid AssignmentId, Guid StudentId, string StudentName, string Content,
    DateTimeOffset SubmittedAt, decimal? Score, string? Feedback, DateTimeOffset? GradedAt)
{
    internal static SubmissionDetail From(SubmissionRecord record) => From(record.Submission, record.StudentName);

    internal static SubmissionDetail From(Submission submission, string studentName) =>
        new(submission.Id, submission.AssignmentId, submission.StudentId, studentName, submission.Content,
            submission.SubmittedAt, submission.Score, submission.Feedback, submission.GradedAt);
}

public sealed record RegisterStudentRequest(string? Name, string? Email, string? Password);
public sealed record LoginRequest(string? Email, string? Password);
public sealed record AccountDto(Guid Id, string Name, string Email, UserRole Role)
{
    internal static AccountDto From(Account account) => new(account.Id, account.Name, account.Email, account.Role);
}

public sealed record GradingInput(
    string Subject, string Description, string? ReferenceAnswer, string Rubric, string Content, decimal MaxScore);

public sealed record GradingSuggestion(decimal Score, string Feedback, string Model);
