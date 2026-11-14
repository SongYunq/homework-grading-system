using HomeworkGrading.Application;
using HomeworkGrading.Domain;
using Microsoft.EntityFrameworkCore;

namespace HomeworkGrading.Infrastructure;

internal sealed class AccountStore(HomeworkDbContext db) : IUserStore
{
    public Task<Account?> FindByEmailAsync(string email, CancellationToken ct) =>
        db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Email == email, ct);

    public async Task AddAsync(Account account, CancellationToken ct)
    {
        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (SqliteErrors.IsUniqueViolation(exception))
        {
            throw UseCaseException.Conflict("email_already_registered", "该邮箱已注册。");
        }
        catch (DbUpdateException exception) when (SqliteErrors.IsBusy(exception))
        {
            throw SqliteErrors.DatabaseBusy();
        }
    }
}
