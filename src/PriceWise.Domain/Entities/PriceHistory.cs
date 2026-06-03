using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class PriceHistory : BaseEntity
{
    private PriceHistory(
        Guid id,
        Guid userId,
        Guid productId,
        Guid storeId,
        decimal price,
        string currency,
        DateTime capturedAt,
        string? sourceUrl,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        ProductId = productId;
        StoreId = storeId;
        Price = price;
        Currency = currency;
        CapturedAt = capturedAt;
        SourceUrl = sourceUrl;
        SetCreatedAt(createdAtUtc);
        SetUpdatedAt(updatedAtUtc);
    }

    private PriceHistory(
        Guid userId,
        Guid productId,
        Guid storeId,
        decimal price,
        string currency,
        DateTime capturedAt,
        string? sourceUrl)
    {
        UserId = userId;
        ProductId = productId;
        StoreId = storeId;
        Price = price;
        Currency = currency;
        CapturedAt = capturedAt;
        SourceUrl = sourceUrl;
    }

    public Guid UserId { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid StoreId { get; private set; }

    public decimal Price { get; private set; }

    public string Currency { get; private set; }

    public DateTime CapturedAt { get; private set; }

    public string? SourceUrl { get; private set; }

    public static PriceHistory Create(
        Guid userId,
        Guid productId,
        Guid storeId,
        decimal price,
        string currency,
        DateTime? capturedAt,
        string? sourceUrl)
    {
        return new PriceHistory(
            userId,
            productId,
            storeId,
            price,
            currency.Trim().ToUpperInvariant(),
            capturedAt ?? DateTime.UtcNow,
            Normalize(sourceUrl));
    }

    public static PriceHistory Restore(
        Guid id,
        Guid userId,
        Guid productId,
        Guid storeId,
        decimal price,
        string currency,
        DateTime capturedAt,
        string? sourceUrl,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        return new PriceHistory(
            id,
            userId,
            productId,
            storeId,
            price,
            currency,
            capturedAt,
            sourceUrl,
            createdAtUtc,
            updatedAtUtc);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
