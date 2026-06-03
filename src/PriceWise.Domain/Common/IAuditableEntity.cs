namespace PriceWise.Domain.Common;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; }

    string? CreatedBy { get; }

    DateTime? UpdatedAtUtc { get; }

    string? UpdatedBy { get; }
}
