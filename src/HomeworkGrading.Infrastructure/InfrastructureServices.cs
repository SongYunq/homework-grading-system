using HomeworkGrading.Application;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkGrading.Infrastructure;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        var sqlite = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };
        if (!sqlite.ShouldSerialize("Default Timeout"))
            sqlite.DefaultTimeout = 5;

        services.AddDbContext<HomeworkDbContext>(options => options.UseSqlite(sqlite.ToString()));
        services.AddScoped<IUserStore, AccountStore>();
        services.AddScoped<IHomeworkStore, HomeworkStore>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        return services;
    }

    public static async Task InitializeInfrastructureAsync(this IServiceProvider services, bool seedDemoData,
        string? teacherEmail, string? teacherPassword, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomeworkDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        await db.Database.MigrateAsync(cancellationToken);
        await DatabaseSeed.RunAsync(db, hasher, clock, seedDemoData, teacherEmail, teacherPassword, cancellationToken);
    }
}
