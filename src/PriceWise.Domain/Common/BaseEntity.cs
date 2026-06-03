namespace PriceWise.Domain.Common;

public abstract class BaseEntity : IAuditableEntity
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; protected init; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public string? UpdatedBy { get; private set; }

    public void MarkCreated(string? createdBy = null)
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public void MarkUpdated(string? updatedBy = null)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
