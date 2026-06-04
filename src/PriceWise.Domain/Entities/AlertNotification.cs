using PriceWise.Domain.Common;

namespace PriceWise.Domain.Entities;

public sealed class AlertNotification : BaseEntity
{
    private AlertNotification(
        Guid id,
        Guid userId,
        Guid priceAlertId,
        Guid productId,
        Guid priceHistoryId,
        decimal triggeredPrice,
        decimal targetPrice,
        DateTime triggeredAt,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        PriceAlertId = priceAlertId;
        ProductId = productId;
        PriceHistoryId = priceHistoryId;
        TriggeredPrice = triggeredPrice;
        TargetPrice = targetPrice;
        TriggeredAt = triggeredAt;
        SetCreatedAt(createdAtUtc);
        SetUpdatedAt(updatedAtUtc);
    }

    private AlertNotification(
        Guid userId,
        Guid priceAlertId,
        Guid productId,
        Guid priceHistoryId,
        decimal triggeredPrice,
        decimal targetPrice,
        DateTime triggeredAt)
    {
        UserId = userId;
        PriceAlertId = priceAlertId;
        ProductId = productId;
        PriceHistoryId = priceHistoryId;
        TriggeredPrice = triggeredPrice;
        TargetPrice = targetPrice;
        TriggeredAt = triggeredAt;
    }

    public Guid UserId { get; private set; }

    public Guid PriceAlertId { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid PriceHistoryId { get; private set; }

    public decimal TriggeredPrice { get; private set; }

    public decimal TargetPrice { get; private set; }

    public DateTime TriggeredAt { get; private set; }

    public static AlertNotification Create(
        Guid userId,
        Guid priceAlertId,
        Guid productId,
        Guid priceHistoryId,
        decimal triggeredPrice,
        decimal targetPrice)
    {
        return new AlertNotification(
            userId,
            priceAlertId,
            productId,
            priceHistoryId,
            triggeredPrice,
            targetPrice,
            DateTime.UtcNow);
    }

    public static AlertNotification Restore(
        Guid id,
        Guid userId,
        Guid priceAlertId,
        Guid productId,
        Guid priceHistoryId,
        decimal triggeredPrice,
        decimal targetPrice,
        DateTime triggeredAt,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        return new AlertNotification(
            id,
            userId,
            priceAlertId,
            productId,
            priceHistoryId,
            triggeredPrice,
            targetPrice,
            triggeredAt,
            createdAtUtc,
            updatedAtUtc);
    }
}
