namespace HomeworkGrading.Domain;

public enum DomainRuleKind
{
    Validation,
    Conflict
}

public sealed class DomainRuleException(DomainRuleKind kind, string code, string message) : Exception(message)
{
    public DomainRuleKind Kind { get; } = kind;
    public string Code { get; } = code;
}

internal static class DomainText
{
    public static string Require(string? value, int limit, string code, string field)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > limit)
            throw new DomainRuleException(DomainRuleKind.Validation, code, $"{field}须填写且不超过 {limit} 个字符。");

        return text;
    }

    public static string? Optional(string? value, int limit, string code, string field)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Require(value, limit, code, field);
    }
}
