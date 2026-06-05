namespace PriceWise.Application.Common;

public sealed record PagedResponse<T>(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyCollection<T> Items,
    bool HasNextPage,
    bool HasPreviousPage)
{
    public static PagedResponse<T> Create(
        IReadOnlyCollection<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResponse<T>(
            page,
            pageSize,
            totalItems,
            totalPages,
            items,
            totalPages > 0 && page < totalPages,
            page > 1 && totalPages > 0);
    }
}
