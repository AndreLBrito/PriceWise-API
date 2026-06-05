namespace PriceWise.Application.Common;

public sealed record ListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? SortBy = null,
    string? SortDirection = null)
{
    public int NormalizedPage => Math.Max(Page, 1);

    public int NormalizedPageSize => Math.Clamp(PageSize, 1, 100);

    public int Offset => (NormalizedPage - 1) * NormalizedPageSize;

    public bool IsDescending => string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
}
