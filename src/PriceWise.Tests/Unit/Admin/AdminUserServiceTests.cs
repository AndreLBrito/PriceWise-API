using FluentAssertions;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Admin;
using PriceWise.Application.Admin.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.Admin;

public sealed class AdminUserServiceTests
{
    [Fact]
    public async Task UpdateRoleAsyncDoesNotRemoveOwnAdminRole()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository());
        var admin = User.CreateAdmin("Admin", "admin@test.com", "hash");
        await userRepository.AddAsync(admin);

        var result = await service.UpdateRoleAsync(
            admin.Id,
            admin.Id,
            new UpdateUserRoleRequest("User"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdminUserErrors.CannotRemoveOwnAdminRole);
    }

    [Fact]
    public async Task DeactivateAsyncDoesNotDeactivateCurrentAdmin()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository());
        var admin = User.CreateAdmin("Admin", "admin@test.com", "hash");
        await userRepository.AddAsync(admin);

        var result = await service.DeactivateAsync(admin.Id, admin.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdminUserErrors.CannotDeactivateSelf);
        admin.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRoleAsyncChangesUserRole()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository());
        var admin = User.CreateAdmin("Admin", "admin@test.com", "hash");
        var user = User.Create("User", "user@test.com", "hash");
        await userRepository.AddAsync(admin);
        await userRepository.AddAsync(user);

        var result = await service.UpdateRoleAsync(
            admin.Id,
            user.Id,
            new UpdateUserRoleRequest("Admin"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Admin");
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task DeactivateAsyncRevokesUserRefreshTokens()
    {
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var service = CreateService(userRepository, refreshTokenRepository);
        var admin = User.CreateAdmin("Admin", "admin@test.com", "hash");
        var user = User.Create("User", "user@test.com", "hash");
        await userRepository.AddAsync(admin);
        await userRepository.AddAsync(user);
        await refreshTokenRepository.AddAsync(RefreshToken.Create(
            user.Id,
            "token-hash",
            DateTime.UtcNow.AddDays(1)));

        var result = await service.DeactivateAsync(admin.Id, user.Id);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        refreshTokenRepository.RefreshTokens.Should().OnlyContain(token => !token.IsActive);
    }

    private static AdminUserService CreateService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        return new AdminUserService(userRepository, refreshTokenRepository);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.Email == email));
        }

        public Task<IReadOnlyCollection<User>> ListAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<User>>(Users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray());
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.Count);
        }

        public Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            Users.Add(entity);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Users.RemoveAll(user => user.Id == id);

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> RefreshTokens { get; } = [];

        public Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RefreshTokens.SingleOrDefault(token => token.TokenHash == tokenHash));
        }

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            RefreshTokens.Add(refreshToken);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RevokeActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            foreach (var token in RefreshTokens.Where(token => token.UserId == userId && token.IsActive))
            {
                token.Revoke();
            }

            return Task.CompletedTask;
        }
    }
}
