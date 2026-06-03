using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class Store : BaseEntity
{
    private Store(
        Guid id,
        Guid userId,
        string name,
        string baseUrl,
        string? logoUrl,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Name = name;
        BaseUrl = baseUrl;
        LogoUrl = logoUrl;
        IsActive = isActive;
        SetCreatedAt(createdAtUtc);
        SetUpdatedAt(updatedAtUtc);
    }

    private Store(Guid userId, string name, string baseUrl, string? logoUrl)
    {
        UserId = userId;
        Name = name;
        BaseUrl = baseUrl;
        LogoUrl = logoUrl;
        IsActive = true;
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public string BaseUrl { get; private set; }

    public string? LogoUrl { get; private set; }

    public bool IsActive { get; private set; }

    public static Store Create(Guid userId, string name, string baseUrl, string? logoUrl)
    {
        return new Store(userId, name.Trim(), baseUrl.Trim(), Normalize(logoUrl));
    }

    public static Store Restore(
        Guid id,
        Guid userId,
        string name,
        string baseUrl,
        string? logoUrl,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        return new Store(id, userId, name, baseUrl, logoUrl, isActive, createdAtUtc, updatedAtUtc);
    }

    public void Update(string name, string baseUrl, string? logoUrl)
    {
        Name = name.Trim();
        BaseUrl = baseUrl.Trim();
        LogoUrl = Normalize(logoUrl);
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
