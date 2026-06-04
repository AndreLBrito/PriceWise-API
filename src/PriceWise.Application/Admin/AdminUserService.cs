using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Admin.Dtos;
using PriceWise.Application.Common;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.Admin;

public sealed class AdminUserService : IAdminUserService
{
    private const int MaxPageSize = 100;

    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenRepository refreshTokenRepository;

    public AdminUserService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        this.userRepository = userRepository;
        this.refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<AdminUserListResponse>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var users = await userRepository.ListAsync(normalizedPage, normalizedPageSize, cancellationToken);
        var totalItems = await userRepository.CountAsync(cancellationToken);

        return Result<AdminUserListResponse>.Success(new AdminUserListResponse(
            users.Select(ToResponse).ToArray(),
            normalizedPage,
            normalizedPageSize,
            totalItems));
    }

    public async Task<Result<AdminUserResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);

        return user is null
            ? Result<AdminUserResponse>.Failure(AdminUserErrors.UserNotFound)
            : Result<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<Result<AdminUserResponse>> UpdateRoleAsync(
        Guid currentAdminUserId,
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result<AdminUserResponse>.Failure(AdminUserErrors.UserNotFound);
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            return Result<AdminUserResponse>.Failure(AdminUserErrors.InvalidRole);
        }

        if (user.Id == currentAdminUserId && user.Role == UserRole.Admin && role != UserRole.Admin)
        {
            return Result<AdminUserResponse>.Failure(AdminUserErrors.CannotRemoveOwnAdminRole);
        }

        user.ChangeRole(role);
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<Result<AdminUserResponse>> ActivateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result<AdminUserResponse>.Failure(AdminUserErrors.UserNotFound);
        }

        user.Activate();
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<Result<AdminUserResponse>> DeactivateAsync(
        Guid currentAdminUserId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result<AdminUserResponse>.Failure(AdminUserErrors.UserNotFound);
        }

        if (user.Id == currentAdminUserId)
        {
            return Result<AdminUserResponse>.Failure(AdminUserErrors.CannotDeactivateSelf);
        }

        user.Deactivate();
        await userRepository.UpdateAsync(user, cancellationToken);
        await refreshTokenRepository.RevokeActiveByUserIdAsync(user.Id, cancellationToken);

        return Result<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<Result> RevokeRefreshTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(AdminUserErrors.UserNotFound);
        }

        await refreshTokenRepository.RevokeActiveByUserIdAsync(userId, cancellationToken);

        return Result.Success();
    }

    private static AdminUserResponse ToResponse(User user)
    {
        return new AdminUserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString(),
            user.IsActive,
            user.FailedLoginAttempts,
            user.LockedUntilUtc,
            user.CreatedAtUtc);
    }
}
