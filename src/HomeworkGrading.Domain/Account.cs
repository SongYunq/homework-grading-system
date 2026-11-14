namespace HomeworkGrading.Domain;

public enum UserRole
{
    Student,
    Teacher
}

public sealed class Account
{
    private Account() { }

    public Account(string name, string email, string passwordHash, UserRole role)
    {
        Id = Guid.NewGuid();
        Name = DomainText.Require(name, 80, "invalid_name", "姓名");
        Email = DomainText.Require(email, 254, "invalid_email", "邮箱").ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
}
