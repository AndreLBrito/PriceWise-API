namespace PriceWise.Application.Admin.Dtos;

public sealed record AdminUserListResponse(
    IReadOnlyCollection<AdminUserResponse> Items,
    int Page,
    int PageSize,
    int TotalItems);
