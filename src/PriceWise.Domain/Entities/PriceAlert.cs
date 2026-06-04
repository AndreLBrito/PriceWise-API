using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class PriceAlert : BaseEntity
{
    private PriceAlert(
        Guid id,
        Guid userId,
        Guid productId,
        decimal targetPrice,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        ProductId = productId;
        TargetPrice = targetPrice;
        IsActive = isActive;
        SetCreatedAt(createdAtUtc);
        SetUpdatedAt(updatedAtUtc);
    }

    private PriceAlert(Guid userId, Guid productId, decimal targetPrice)
    {
        UserId = userId;
        ProductId = productId;
        TargetPrice = targetPrice;
        IsActive = true;
    }

    public Guid UserId { get; private set; }

    public Guid ProductId { get; private set; }

    public decimal TargetPrice { get; private set; }

    public bool IsActive { get; private set; }

    public static PriceAlert Create(Guid userId, Guid productId, decimal targetPrice)
    {
        return new PriceAlert(userId, productId, targetPrice);
    }

    public static PriceAlert Restore(
        Guid id,
        Guid userId,
        Guid productId,
        decimal targetPrice,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        return new PriceAlert(id, userId, productId, targetPrice, isActive, createdAtUtc, updatedAtUtc);
    }

    public void Update(decimal targetPrice)
    {
        TargetPrice = targetPrice;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
