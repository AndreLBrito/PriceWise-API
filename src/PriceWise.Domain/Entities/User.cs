using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class User : BaseEntity
{
    private User(
        Guid id,
        string name,
        string email,
        string passwordHash,
        bool isActive,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = isActive;
        SetCreatedAt(createdAtUtc);
    }

    private User(string name, string email, string passwordHash)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public static User Create(string name, string email, string passwordHash)
    {
        return new User(name, email.ToLowerInvariant(), passwordHash);
    }

    public static User Restore(
        Guid id,
        string name,
        string email,
        string passwordHash,
        bool isActive,
        DateTime createdAtUtc)
    {
        return new User(id, name, email, passwordHash, isActive, createdAtUtc);
    }
}
