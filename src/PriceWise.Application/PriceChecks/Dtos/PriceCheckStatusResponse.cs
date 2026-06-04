namespace PriceWise.Application.PriceChecks.Dtos;

public sealed record PriceCheckStatusResponse(
    bool Enabled,
    int IntervalInMinutes,
    int MaxProductsPerExecution,
    DateTime? LastExecutionAt,
    string? LastExecutionStatus,
    string? LastExecutionMessage);
