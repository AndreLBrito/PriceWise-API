using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class User : BaseEntity
{
    private User(
        Guid id,
        string name,
        string email,
        string passwordHash,
        UserRole role,
        bool isActive,
        int failedLoginAttempts,
        DateTime? lockedUntilUtc,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = isActive;
        FailedLoginAttempts = failedLoginAttempts;
        LockedUntilUtc = lockedUntilUtc;
        SetCreatedAt(createdAtUtc);
    }

    private User(string name, string email, string passwordHash, UserRole role)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        FailedLoginAttempts = 0;
    }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public static User Create(string name, string email, string passwordHash)
    {
        return new User(name, email.ToLowerInvariant(), passwordHash, UserRole.User);
    }

    public static User CreateAdmin(string name, string email, string passwordHash)
    {
        return new User(name, email.ToLowerInvariant(), passwordHash, UserRole.Admin);
    }

    public static User Restore(
        Guid id,
        string name,
        string email,
        string passwordHash,
        UserRole role,
        bool isActive,
        int failedLoginAttempts,
        DateTime? lockedUntilUtc,
        DateTime createdAtUtc)
    {
        return new User(id, name, email, passwordHash, role, isActive, failedLoginAttempts, lockedUntilUtc, createdAtUtc);
    }

    public bool IsLocked(DateTime utcNow)
    {
        return LockedUntilUtc is not null && LockedUntilUtc > utcNow;
    }

    public void RegisterFailedLogin(int maxAttempts, int lockoutMinutes)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedUntilUtc = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }

        MarkUpdated();
    }

    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        MarkUpdated();
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MarkUpdated();
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
