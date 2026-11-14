using HomeworkGrading.Application;
using HomeworkGrading.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HomeworkGrading.Infrastructure;

internal sealed class HomeworkStore(HomeworkDbContext db) : IHomeworkStore
{
    public Task<AssignmentList> ListAsync(Actor actor, AssignmentListQuery query, DateTimeOffset now, CancellationToken ct) =>
        ReadSnapshotAsync(() => LoadAssignmentPageAsync(actor, query, now, ct), ct);

    private async Task<AssignmentList> LoadAssignmentPageAsync(Actor actor, AssignmentListQuery query, DateTimeOffset now, CancellationToken ct)
    {
        var rows = actor.Role == UserRole.Teacher ? TeacherRows(actor.Id, now) : StudentRows(actor.Id, now);
        var counts = await rows.GroupBy(row => row.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, ct);
        var summary = new AssignmentSummary(
            counts.GetValueOrDefault(AssignmentStatus.Open),
            counts.GetValueOrDefault(AssignmentStatus.Closed),
            counts.GetValueOrDefault(AssignmentStatus.Pending),
            counts.GetValueOrDefault(AssignmentStatus.Graded));

        if (query.Status is { } status)
            rows = rows.Where(row => row.Status == status);

        var items = await rows.OrderByDescending(row => row.CreatedAt).ThenByDescending(row => row.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(row => new AssignmentListItem(row.Id, row.Title, row.Subject, row.MaxScore, row.DueAt,
                row.CreatedAt, row.Status, row.SubmissionCount, row.GradedCount))
            .ToListAsync(ct);
        var totalCount = query.Status is { } selected ? counts.GetValueOrDefault(selected) : counts.Values.Sum();
        return new AssignmentList(items, query.Page, query.PageSize, totalCount, summary);
    }

    public Task<Assignment?> FindAssignmentAsync(Guid id, CancellationToken ct) =>
        db.Assignments.SingleOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> HasSubmissionsAsync(Guid assignmentId, CancellationToken ct) =>
        db.Submissions.AnyAsync(s => s.AssignmentId == assignmentId, ct);

    public Task<SubmissionPage> ListSubmissionsAsync(Guid assignmentId, PageQuery page, CancellationToken ct) =>
        ReadSnapshotAsync(() => LoadSubmissionPageAsync(assignmentId, page, ct), ct);

    private async Task<SubmissionPage> LoadSubmissionPageAsync(Guid assignmentId, PageQuery page, CancellationToken ct)
    {
        var submissions = db.Submissions.Where(s => s.AssignmentId == assignmentId);
        var count = await submissions.CountAsync(ct);
        var items = await submissions.OrderByDescending(s => s.SubmittedAt).ThenByDescending(s => s.Id)
            .Skip((page.Page - 1) * page.PageSize).Take(page.PageSize)
            .Join(db.Accounts, s => s.StudentId, a => a.Id,
                (s, a) => new SubmissionSummary(s.Id, s.StudentId, a.Name, s.SubmittedAt, s.Score, s.GradedAt))
            .ToListAsync(ct);
        return new SubmissionPage(items, page.Page, page.PageSize, count);
    }

    public Task<SubmissionRecord?> FindStudentSubmissionAsync(Guid assignmentId, Guid studentId, CancellationToken ct) =>
        FindSubmissionAsync(db.Submissions.Where(s => s.AssignmentId == assignmentId && s.StudentId == studentId), ct);

    public Task<SubmissionRecord?> FindSubmissionAsync(Guid id, CancellationToken ct) =>
        FindSubmissionAsync(db.Submissions.Where(s => s.Id == id), ct);

    public void Add(Assignment assignment) => db.Assignments.Add(assignment);
    public void Add(Submission submission) => db.Submissions.Add(submission);
    public void Remove(Assignment assignment) => db.Assignments.Remove(assignment);

    public async Task<T> ExecuteWriteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        try
        {
            // Microsoft.Data.Sqlite starts an IMMEDIATE transaction by default, so validation and the write share one lock.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await operation(ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateException exception) when (SqliteErrors.IsUniqueViolation(exception))
        {
            throw UseCaseException.Conflict("submission_already_exists", "该作业已提交，不能重复提交。");
        }
        catch (Exception exception) when (SqliteErrors.IsBusy(exception))
        {
            throw SqliteErrors.DatabaseBusy();
        }
    }

    private async Task<T> ReadSnapshotAsync<T>(Func<Task<T>> read, CancellationToken ct)
    {
        try
        {
            await db.Database.OpenConnectionAsync(ct);
            try
            {
                var connection = (SqliteConnection)db.Database.GetDbConnection();
                // A deferred read transaction gives count and page queries the same snapshot without taking a writer lock.
                await using var transaction = connection.BeginTransaction(deferred: true);
                await using var enlistment = await db.Database.UseTransactionAsync(transaction, ct);
                var result = await read();
                await transaction.CommitAsync(ct);
                return result;
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
        catch (SqliteException exception) when (SqliteErrors.IsBusy(exception))
        {
            throw SqliteErrors.DatabaseBusy();
        }
    }

    private Task<SubmissionRecord?> FindSubmissionAsync(IQueryable<Submission> submissions, CancellationToken ct) =>
        submissions.Join(db.Accounts, s => s.StudentId, a => a.Id,
            (s, a) => new SubmissionRecord(s, a.Name)).SingleOrDefaultAsync(ct);

    private IQueryable<AssignmentRow> TeacherRows(Guid teacherId, DateTimeOffset now)
    {
        var stats = from submission in db.Submissions
            join assignment in db.Assignments.Where(a => a.TeacherId == teacherId)
                on submission.AssignmentId equals assignment.Id
            group submission by submission.AssignmentId into groupRows
            select new
            {
                Id = groupRows.Key,
                SubmissionCount = (int?)groupRows.Count(),
                GradedCount = (int?)groupRows.Count(s => s.Score != null)
            };

        return from assignment in db.Assignments
            where assignment.TeacherId == teacherId
            join stat in stats on assignment.Id equals stat.Id into assignmentStats
            from stat in assignmentStats.DefaultIfEmpty()
            let submissionCount = stat.SubmissionCount ?? 0
            let gradedCount = stat.GradedCount ?? 0
            select new AssignmentRow
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Subject = assignment.Subject,
                MaxScore = assignment.MaxScore,
                DueAt = assignment.DueAt,
                CreatedAt = assignment.CreatedAt,
                SubmissionCount = submissionCount,
                GradedCount = gradedCount,
                Status = submissionCount > gradedCount ? AssignmentStatus.Pending
                    : gradedCount > 0 ? AssignmentStatus.Graded
                    : assignment.DueAt > now ? AssignmentStatus.Open : AssignmentStatus.Closed
            };
    }

    private IQueryable<AssignmentRow> StudentRows(Guid studentId, DateTimeOffset now) =>
        from assignment in db.Assignments
        join submission in db.Submissions.Where(s => s.StudentId == studentId)
            on assignment.Id equals submission.AssignmentId into ownSubmissions
        from submission in ownSubmissions.DefaultIfEmpty()
        select new AssignmentRow
        {
            Id = assignment.Id,
            Title = assignment.Title,
            Subject = assignment.Subject,
            MaxScore = assignment.MaxScore,
            DueAt = assignment.DueAt,
            CreatedAt = assignment.CreatedAt,
            Status = submission != null ? (submission.Score != null ? AssignmentStatus.Graded : AssignmentStatus.Pending)
                : assignment.DueAt > now ? AssignmentStatus.Open : AssignmentStatus.Closed
        };

    private sealed class AssignmentRow
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public required string Subject { get; init; }
        public decimal MaxScore { get; init; }
        public DateTimeOffset DueAt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public AssignmentStatus Status { get; init; }
        public int? SubmissionCount { get; init; }
        public int? GradedCount { get; init; }
    }
}
