using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        RevokedAtUtc = revokedAtUtc;
        SetCreatedAt(createdAtUtc);
    }

    private RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        return new RefreshToken(userId, tokenHash, expiresAtUtc);
    }

    public static RefreshToken Restore(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc)
    {
        return new RefreshToken(id, userId, tokenHash, expiresAtUtc, revokedAtUtc, createdAtUtc);
    }

    public void Revoke()
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
