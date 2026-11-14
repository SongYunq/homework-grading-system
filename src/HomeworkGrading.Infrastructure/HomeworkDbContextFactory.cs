using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeworkGrading.Infrastructure;

internal sealed class HomeworkDbContextFactory : IDesignTimeDbContextFactory<HomeworkDbContext>
{
    public HomeworkDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Homework")
            ?? "Data Source=homework-design.db;Foreign Keys=True";
        var options = new DbContextOptionsBuilder<HomeworkDbContext>().UseSqlite(connectionString).Options;
        return new HomeworkDbContext(options);
    }
}
