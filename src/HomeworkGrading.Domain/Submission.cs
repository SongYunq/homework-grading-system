namespace HomeworkGrading.Domain;

public sealed class Submission
{
    private Submission() { }

    public Submission(Guid assignmentId, Guid studentId, string content, DateTimeOffset submittedAt)
    {
        Id = Guid.NewGuid();
        AssignmentId = assignmentId;
        StudentId = studentId;
        Content = DomainText.Require(content, 30_000, "invalid_content", "作答内容");
        SubmittedAt = submittedAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid StudentId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTimeOffset SubmittedAt { get; private set; }
    public decimal? Score { get; private set; }
    public string? Feedback { get; private set; }
    public DateTimeOffset? GradedAt { get; private set; }

    public void Grade(decimal score, string feedback, decimal maxScore, DateTimeOffset now)
    {
        if (!IsValidScore(score, maxScore))
            throw new DomainRuleException(DomainRuleKind.Validation, "invalid_score",
                "分数须介于 0 与作业满分之间，最多保留两位小数。");

        var text = DomainText.Require(feedback, 5_000, "invalid_feedback", "批改反馈");
        if (Score == score && Feedback == text)
            return;

        Feedback = text;
        Score = score;
        GradedAt = now.ToUniversalTime();
    }

    public static bool IsValidScore(decimal score, decimal maxScore) =>
        score >= 0 && score <= maxScore && decimal.Round(score, 2) == score;
}
