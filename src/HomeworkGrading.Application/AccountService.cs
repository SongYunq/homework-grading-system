using System.Net.Mail;
using HomeworkGrading.Domain;

namespace HomeworkGrading.Application;

public sealed class AccountService(IUserStore users, IPasswordHasher passwordHasher)
{
    public async Task<AccountDto> RegisterStudentAsync(RegisterStudentRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 80)
            throw UseCaseException.Validation("invalid_name", "姓名须填写且不超过 80 个字符。");
        if (request.Password is not { Length: >= 8 and <= 128 } password || string.IsNullOrWhiteSpace(password))
            throw UseCaseException.Validation("invalid_password", "密码须为 8 至 128 个字符，不能全部为空白。");

        if (await users.FindByEmailAsync(email, ct) is not null)
            throw UseCaseException.Conflict("email_already_registered", "该邮箱已注册。");

        var account = new Account(name, email, passwordHasher.Hash(password), UserRole.Student);
        await users.AddAsync(account, ct);
        return AccountDto.From(account);
    }

    public async Task<AccountDto> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (request.Password is not { Length: > 0 and <= 128 } password)
            throw InvalidCredentials();

        var account = await users.FindByEmailAsync(email, ct);
        if (account is null || !passwordHasher.Verify(account.PasswordHash, password))
            throw InvalidCredentials();

        return AccountDto.From(account);
    }

    private static string NormalizeEmail(string? email)
    {
        var value = email?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 254 ||
            !MailAddress.TryCreate(value, out var address) ||
            !string.Equals(address.Address, value, StringComparison.Ordinal))
        {
            throw UseCaseException.Validation("invalid_email", "请输入有效邮箱。");
        }

        return value.ToLowerInvariant();
    }

    private static UseCaseException InvalidCredentials() =>
        new(UseCaseError.Unauthorized, "invalid_credentials", "邮箱或密码不正确。");
}
