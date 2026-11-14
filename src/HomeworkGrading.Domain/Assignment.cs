namespace HomeworkGrading.Domain;

public sealed record AssignmentDraft(
    string Title, string Subject, string Description, string? ReferenceAnswer,
    string? Rubric, decimal MaxScore, DateTimeOffset DueAt);

public sealed class Assignment
{
    private Assignment() { }

    public Assignment(Guid teacherId, AssignmentDraft draft, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        TeacherId = teacherId;
        CreatedAt = now.ToUniversalTime();
        ApplyDraft(draft, now);
    }

    public Guid Id { get; private set; }
    public Guid TeacherId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? ReferenceAnswer { get; private set; }
    public string? Rubric { get; private set; }
    public decimal MaxScore { get; private set; }
    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Revise(AssignmentDraft draft, bool hasSubmissions, DateTimeOffset now)
    {
        EnsureNoSubmissions(hasSubmissions, "修改");
        ApplyDraft(draft, now);
    }

    public void EnsureCanDelete(bool hasSubmissions)
    {
        EnsureNoSubmissions(hasSubmissions, "删除");
    }

    public Submission AcceptSubmission(Guid studentId, string content, bool alreadySubmitted, DateTimeOffset now)
    {
        if (alreadySubmitted)
            throw new DomainRuleException(DomainRuleKind.Conflict, "submission_already_exists", "你已提交这份作业。");
        if (now >= DueAt)
            throw new DomainRuleException(DomainRuleKind.Conflict, "assignment_closed", "作业已截止，无法继续提交。");

        return new Submission(Id, studentId, content, now);
    }

    private static void EnsureNoSubmissions(bool hasSubmissions, string action)
    {
        if (hasSubmissions)
            throw new DomainRuleException(DomainRuleKind.Conflict, "assignment_has_submissions",
                $"这份作业已有提交，不能{action}。");
    }

    private void ApplyDraft(AssignmentDraft draft, DateTimeOffset now)
    {
        var title = DomainText.Require(draft.Title, 160, "invalid_title", "标题");
        var subject = DomainText.Require(draft.Subject, 40, "invalid_subject", "学科");
        var description = DomainText.Require(draft.Description, 10_000, "invalid_description", "作业内容");
        var referenceAnswer = DomainText.Optional(draft.ReferenceAnswer, 10_000, "invalid_reference_answer", "参考答案");
        var rubric = DomainText.Optional(draft.Rubric, 5_000, "invalid_rubric", "评分标准");
        if (draft.MaxScore is <= 0 or > 1_000 || decimal.Round(draft.MaxScore, 2) != draft.MaxScore)
            throw new DomainRuleException(DomainRuleKind.Validation, "invalid_max_score",
                "满分须大于 0 且不超过 1000，最多保留两位小数。");
        if (draft.DueAt <= now)
            throw new DomainRuleException(DomainRuleKind.Validation, "invalid_due_at", "截止时间须晚于当前时间。");

        Title = title;
        Subject = subject;
        Description = description;
        ReferenceAnswer = referenceAnswer;
        Rubric = rubric;
        MaxScore = draft.MaxScore;
        DueAt = draft.DueAt.ToUniversalTime();
    }
}
