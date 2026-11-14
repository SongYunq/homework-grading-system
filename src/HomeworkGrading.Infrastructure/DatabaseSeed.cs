using System.Net.Mail;
using HomeworkGrading.Application;
using HomeworkGrading.Domain;
using Microsoft.EntityFrameworkCore;

namespace HomeworkGrading.Infrastructure;

internal static class DatabaseSeed
{
    private const string DemoPassword = "Demo123!";

    public static async Task RunAsync(HomeworkDbContext db, IPasswordHasher hasher, TimeProvider clock, bool seedDemoData,
        string? teacherEmail, string? teacherPassword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(teacherEmail) && string.IsNullOrWhiteSpace(teacherPassword))
        {
            if (seedDemoData)
                await AddDemoDataAsync(db, hasher, clock.GetUtcNow(), ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(teacherEmail) || teacherEmail.Length > 254 ||
            !MailAddress.TryCreate(teacherEmail.Trim(), out var address) || address.Address != teacherEmail.Trim() ||
            string.IsNullOrWhiteSpace(teacherPassword) || teacherPassword.Length is < 8 or > 128)
        {
            throw new InvalidOperationException("Bootstrap 教师配置必须包含有效邮箱和长度为 8 至 128 的密码。");
        }

        var email = address.Address.ToLowerInvariant();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await db.Accounts.AnyAsync(a => a.Role == UserRole.Teacher, ct))
            return;

        if (await db.Accounts.AnyAsync(a => a.Email == email, ct))
            throw new InvalidOperationException("Bootstrap 教师邮箱已经被注册为学生，请配置其他邮箱。");

        db.Accounts.Add(new Account("教师", email, hasher.Hash(teacherPassword), UserRole.Teacher));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static async Task AddDemoDataAsync(HomeworkDbContext db, IPasswordHasher hasher, DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await db.Accounts.AnyAsync(ct))
            return;

        var teacher = new Account("林老师", "teacher@example.test", hasher.Hash(DemoPassword), UserRole.Teacher);
        var otherTeacher = new Account("周老师", "teacher2@example.test", hasher.Hash(DemoPassword), UserRole.Teacher);
        var student = new Account("陈一禾", "student@example.test", hasher.Hash(DemoPassword), UserRole.Student);
        var otherStudent = new Account("许知夏", "student2@example.test", hasher.Hash(DemoPassword), UserRole.Student);
        db.Accounts.AddRange(teacher, otherTeacher, student, otherStudent);

        var math = new Assignment(teacher.Id, new AssignmentDraft("一元一次方程", "数学",
            "解方程 3(x - 2) + 4 = 2x + 9，写出步骤并代入检验。", "x = 11，代入后等式两边均为 31。",
            "展开括号、合并移项、求得正确答案和代入检验各占 25 分。", 100, now.AddDays(7)), now.AddDays(-2));
        var chinese = new Assignment(teacher.Id, new AssignmentDraft("放学路上", "语文",
            "选取一处具体景物，运用至少两种感官描写，写一个生活片段。", null,
            "贴合题意 30 分；感官细节 40 分；表达清晰 30 分。", 100, now.AddDays(5)), now.AddDays(-1));
        var history = new Assignment(otherTeacher.Id, new AssignmentDraft("丝绸之路", "历史",
            "简述张骞通西域的意义，并举一个丝绸之路促进交流的例子。", null, null, 100, now.AddDays(10)), now);
        db.Assignments.AddRange(math, chinese, history);

        var pending = new Submission(math.Id, student.Id, "展开后 3x - 2 = 2x + 9，所以 x = 11。", now.AddHours(-3));
        var graded = new Submission(math.Id, otherStudent.Id, "展开并移项得 x = 11。代入后两边都等于 31。", now.AddHours(-6));
        graded.Grade(100, "求解和验算都正确。", math.MaxScore, now.AddHours(-1));
        db.Submissions.AddRange(pending, graded);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
