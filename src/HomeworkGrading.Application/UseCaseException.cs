using HomeworkGrading.Domain;

namespace HomeworkGrading.Application;

public enum UseCaseError
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Unavailable,
    BadGateway,
    GatewayTimeout
}

public sealed class UseCaseException(UseCaseError status, string code, string message) : Exception(message)
{
    public UseCaseError Status { get; } = status;
    public string Code { get; } = code;

    public static UseCaseException Validation(string code, string message) => new(UseCaseError.Validation, code, message);
    public static UseCaseException Conflict(string code, string message) => new(UseCaseError.Conflict, code, message);
    public static UseCaseException NotFound(string code, string message) => new(UseCaseError.NotFound, code, message);
    public static UseCaseException Forbidden(string code, string message) => new(UseCaseError.Forbidden, code, message);

    internal static T ApplyDomain<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (DomainRuleException exception)
        {
            throw From(exception);
        }
    }

    internal static void ApplyDomain(Action action) => ApplyDomain(() =>
    {
        action();
        return true;
    });

    private static UseCaseException From(DomainRuleException exception) => exception.Kind switch
    {
        DomainRuleKind.Validation => Validation(exception.Code, exception.Message),
        DomainRuleKind.Conflict => Conflict(exception.Code, exception.Message),
        _ => throw new ArgumentOutOfRangeException(nameof(exception))
    };
}
