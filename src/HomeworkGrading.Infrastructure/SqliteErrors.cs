using HomeworkGrading.Application;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HomeworkGrading.Infrastructure;

internal static class SqliteErrors
{
    private const int ConstraintUnique = 2067;
    private const int Busy = 5;
    private const int Locked = 6;

    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: ConstraintUnique };

    public static bool IsBusy(Exception exception) => exception switch
    {
        SqliteException { SqliteErrorCode: Busy or Locked } => true,
        DbUpdateException { InnerException: SqliteException { SqliteErrorCode: Busy or Locked } } => true,
        _ => false
    };

    public static UseCaseException DatabaseBusy() =>
        new(UseCaseError.Unavailable, "database_busy", "数据库暂时繁忙，请稍后重试。");
}
