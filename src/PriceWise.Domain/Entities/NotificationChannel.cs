using PriceWise.Domain.Common;
using PriceWise.Domain.Enums;

namespace PriceWise.Domain.Entities;

public sealed class NotificationChannel : BaseEntity
{
    private NotificationChannel(
        Guid id,
        Guid userId,
        NotificationChannelType type,
        string name,
        string destination,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Name = name;
        Destination = destination;
        IsActive = isActive;
        SetCreatedAt(createdAtUtc);
        SetUpdatedAt(updatedAtUtc);
    }

    private NotificationChannel(
        Guid userId,
        NotificationChannelType type,
        string name,
        string destination)
    {
        UserId = userId;
        Type = type;
        Name = name;
        Destination = destination;
        IsActive = true;
    }

    public Guid UserId { get; private set; }

    public NotificationChannelType Type { get; private set; }

    public string Name { get; private set; }

    public string Destination { get; private set; }

    public bool IsActive { get; private set; }

    public static NotificationChannel Create(
        Guid userId,
        NotificationChannelType type,
        string name,
        string destination)
    {
        return new NotificationChannel(userId, type, name.Trim(), destination.Trim());
    }

    public static NotificationChannel Restore(
        Guid id,
        Guid userId,
        NotificationChannelType type,
        string name,
        string destination,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        return new NotificationChannel(
            id,
            userId,
            type,
            name,
            destination,
            isActive,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Update(NotificationChannelType type, string name, string destination)
    {
        Type = type;
        Name = name.Trim();
        Destination = destination.Trim();
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
