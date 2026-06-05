using PriceWise.Application.Admin.Dtos;
using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;

namespace PriceWise.Application.Admin;

public interface IAdminUserService : IService
{
    Task<Result<PagedResponse<AdminUserResponse>>> ListAsync(
        ListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<AdminUserResponse>> UpdateRoleAsync(
        Guid currentAdminUserId,
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserResponse>> ActivateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<AdminUserResponse>> DeactivateAsync(
        Guid currentAdminUserId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
