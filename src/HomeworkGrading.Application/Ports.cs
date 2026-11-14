using HomeworkGrading.Domain;

namespace HomeworkGrading.Application;

public sealed record SubmissionRecord(Submission Submission, string StudentName);

public interface IUserStore
{
    Task<Account?> FindByEmailAsync(string email, CancellationToken ct);
    Task AddAsync(Account account, CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public interface IGradingAdvisor
{
    Task<GradingSuggestion> SuggestAsync(GradingInput input, CancellationToken ct);
}

public interface IHomeworkStore
{
    Task<AssignmentList> ListAsync(Actor actor, AssignmentListQuery query, DateTimeOffset now, CancellationToken ct);
    Task<Assignment?> FindAssignmentAsync(Guid id, CancellationToken ct);
    Task<bool> HasSubmissionsAsync(Guid assignmentId, CancellationToken ct);
    Task<SubmissionPage> ListSubmissionsAsync(Guid assignmentId, PageQuery page, CancellationToken ct);
    Task<SubmissionRecord?> FindStudentSubmissionAsync(Guid assignmentId, Guid studentId, CancellationToken ct);
    Task<SubmissionRecord?> FindSubmissionAsync(Guid id, CancellationToken ct);
    void Add(Assignment assignment);
    void Add(Submission submission);
    void Remove(Assignment assignment);

    /// <summary>Runs the operation's reads and writes together, saves changes, and commits on success.</summary>
    Task<T> ExecuteWriteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
}
