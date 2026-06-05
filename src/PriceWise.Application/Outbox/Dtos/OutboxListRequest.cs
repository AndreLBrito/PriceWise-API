using PriceWise.Application.Common;
using PriceWise.Domain.Enums;

namespace PriceWise.Application.Outbox.Dtos;

public sealed record OutboxListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    OutboxMessageStatus? Status = null,
    string? Type = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? SortBy = null,
    string? SortDirection = null)
{
    public ListRequest ToListRequest()
    {
        return new ListRequest(Page, PageSize, Search, null, StartDate, EndDate, SortBy, SortDirection);
    }
}
