using PriceWise.Application.Common;

namespace PriceWise.Application.Auditing.Dtos;

public sealed record AuditLogListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? SortBy = null,
    string? SortDirection = null,
    Guid? UserId = null,
    string? Action = null,
    string? EntityName = null,
    Guid? EntityId = null)
{
    public ListRequest ToListRequest()
    {
        return new ListRequest(Page, PageSize, Search, IsActive, StartDate, EndDate, SortBy, SortDirection);
    }
}
