using HomeworkGrading.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeworkGrading.Infrastructure;

public sealed class HomeworkDbContext(DbContextOptions<HomeworkDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utcTicks = new ValueConverter<DateTimeOffset, long>(
            value => value.UtcDateTime.Ticks,
            value => new DateTimeOffset(value, TimeSpan.Zero));
        var scoreUnits = new ValueConverter<decimal, long>(
            value => (long)(value * 100m),
            value => value / 100m);

        modelBuilder.Entity<Account>(account =>
        {
            account.ToTable("accounts");
            account.HasKey(a => a.Id);
            account.Property(a => a.Id).ValueGeneratedNever();
            account.Property(a => a.Name).HasMaxLength(80).IsRequired();
            account.Property(a => a.Email).HasMaxLength(254).UseCollation("NOCASE").IsRequired();
            account.Property(a => a.PasswordHash).IsRequired();
            account.Property(a => a.Role).HasConversion<string>().HasMaxLength(16).IsRequired();
            account.HasIndex(a => a.Email).IsUnique();
        });

        modelBuilder.Entity<Assignment>(assignment =>
        {
            assignment.ToTable("assignments", table =>
                table.HasCheckConstraint("CK_assignments_MaxScore", "\"MaxScore\" > 0"));
            assignment.HasKey(a => a.Id);
            assignment.Property(a => a.Id).ValueGeneratedNever();
            assignment.Property(a => a.Title).HasMaxLength(160).IsRequired();
            assignment.Property(a => a.Subject).HasMaxLength(40).IsRequired();
            assignment.Property(a => a.Description).HasMaxLength(10_000).IsRequired();
            assignment.Property(a => a.ReferenceAnswer).HasMaxLength(10_000);
            assignment.Property(a => a.Rubric).HasMaxLength(5_000);
            assignment.Property(a => a.MaxScore).HasConversion(scoreUnits);
            assignment.Property(a => a.DueAt).HasConversion(utcTicks);
            assignment.Property(a => a.CreatedAt).HasConversion(utcTicks);
            assignment.HasOne<Account>().WithMany().HasForeignKey(a => a.TeacherId).OnDelete(DeleteBehavior.Restrict);
            assignment.HasIndex(a => new { a.CreatedAt, a.Id });
            assignment.HasIndex(a => new { a.TeacherId, a.CreatedAt, a.Id });
        });

        modelBuilder.Entity<Submission>(submission =>
        {
            submission.ToTable("submissions", table =>
            {
                table.HasCheckConstraint("CK_submissions_Score", "\"Score\" IS NULL OR \"Score\" >= 0");
                table.HasCheckConstraint("CK_submissions_GradeFields", """
                    ("Score" IS NULL AND "Feedback" IS NULL AND "GradedAt" IS NULL)
                    OR ("Score" IS NOT NULL AND "Feedback" IS NOT NULL AND "GradedAt" IS NOT NULL)
                    """);
            });
            submission.HasKey(s => s.Id);
            submission.Property(s => s.Id).ValueGeneratedNever();
            submission.Property(s => s.Content).HasMaxLength(30_000).IsRequired();
            submission.Property(s => s.Feedback).HasMaxLength(5_000);
            submission.Property(s => s.Score).HasConversion(scoreUnits);
            submission.Property(s => s.SubmittedAt).HasConversion(utcTicks);
            submission.Property(s => s.GradedAt).HasConversion(utcTicks);
            submission.HasOne<Assignment>().WithMany().HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Restrict);
            submission.HasOne<Account>().WithMany().HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
            submission.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
            submission.HasIndex(s => new { s.AssignmentId, s.SubmittedAt, s.Id });
        });
    }
}
