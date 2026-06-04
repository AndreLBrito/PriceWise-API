using FluentAssertions;
using Microsoft.Extensions.Options;
using PriceWise.Application.Abstractions.Auth;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Abstractions.Telemetry;
using PriceWise.Application.Authentication;
using PriceWise.Application.Authentication.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Tests.Unit.Authentication;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsyncCreatesUserAndReturnsTokens()
    {
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var service = CreateService(userRepository, refreshTokenRepository);

        var result = await service.RegisterAsync(new RegisterRequest(
            "Andre",
            "andre@test.com",
            "password123"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("andre@test.com");
        result.Value.Role.Should().Be("User");
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        userRepository.Users.Should().ContainSingle();
        refreshTokenRepository.RefreshTokens.Should().ContainSingle();
    }

    [Fact]
    public async Task RegisterAsyncFailsWhenEmailAlreadyExists()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository());
        await userRepository.AddAsync(User.Create("Andre", "andre@test.com", "hash"));

        var result = await service.RegisterAsync(new RegisterRequest(
            "Andre",
            "andre@test.com",
            "password123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.EmailAlreadyRegistered);
    }

    [Fact]
    public async Task LoginAsyncFailsWhenPasswordIsInvalid()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository());
        await userRepository.AddAsync(User.Create("Andre", "andre@test.com", "hashed-password123"));

        var result = await service.LoginAsync(new LoginRequest("andre@test.com", "wrong-password"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
        userRepository.Users.Single().FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsyncFailsWhenUserIsInactive()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository());
        var user = User.Create("Andre", "andre@test.com", "hashed-password123");
        user.Deactivate();
        await userRepository.AddAsync(user);

        var result = await service.LoginAsync(new LoginRequest("andre@test.com", "password123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.UserInactive);
    }

    [Fact]
    public async Task LoginAsyncLocksUserAfterConfiguredFailedAttempts()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository(), maxFailedLoginAttempts: 2);
        var user = User.Create("Andre", "andre@test.com", "hashed-password123");
        await userRepository.AddAsync(user);

        await service.LoginAsync(new LoginRequest("andre@test.com", "wrong-password"));
        var result = await service.LoginAsync(new LoginRequest("andre@test.com", "wrong-password"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
        user.FailedLoginAttempts.Should().Be(2);
        user.LockedUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsyncResetsFailedAttemptsWhenCredentialsAreValid()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository());
        var user = User.Create("Andre", "andre@test.com", "hashed-password123");
        user.RegisterFailedLogin(5, 15);
        await userRepository.AddAsync(user);

        var result = await service.LoginAsync(new LoginRequest("andre@test.com", "password123"));

        result.IsSuccess.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsyncPreservesCurrentUserRole()
    {
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var service = CreateService(userRepository, refreshTokenRepository);
        var user = User.CreateAdmin("Admin", "admin@test.com", "hashed-password123");
        await userRepository.AddAsync(user);
        await refreshTokenRepository.AddAsync(RefreshToken.Create(
            user.Id,
            "hashed-refresh-token",
            DateTime.UtcNow.AddDays(1)));

        var result = await service.RefreshTokenAsync(new RefreshTokenRequest("refresh-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task ChangePasswordAsyncFailsWhenCurrentPasswordIsInvalid()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateService(userRepository, new InMemoryRefreshTokenRepository());
        var user = User.Create("Andre", "andre@test.com", "hashed-password123");
        await userRepository.AddAsync(user);

        var result = await service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest("wrong-password", "new-password123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCurrentPassword);
    }

    [Fact]
    public async Task ChangePasswordAsyncChangesPasswordAndRevokesRefreshTokens()
    {
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var service = CreateService(userRepository, refreshTokenRepository);
        var user = User.Create("Andre", "andre@test.com", "hashed-password123");
        await userRepository.AddAsync(user);
        await refreshTokenRepository.AddAsync(RefreshToken.Create(
            user.Id,
            "hashed-refresh-token",
            DateTime.UtcNow.AddDays(1)));

        var result = await service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest("password123", "new-password123"));

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed-new-password123");
        refreshTokenRepository.RefreshTokens.Should().OnlyContain(token => !token.IsActive);
    }

    [Fact]
    public async Task RevokeRefreshTokensAsyncRevokesActiveTokens()
    {
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var service = CreateService(userRepository, refreshTokenRepository);
        var user = User.Create("Andre", "andre@test.com", "hashed-password123");
        await userRepository.AddAsync(user);
        await refreshTokenRepository.AddAsync(RefreshToken.Create(
            user.Id,
            "hashed-refresh-token",
            DateTime.UtcNow.AddDays(1)));

        var result = await service.RevokeRefreshTokensAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        refreshTokenRepository.RefreshTokens.Should().OnlyContain(token => !token.IsActive);
    }

    private static AuthService CreateService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        int maxFailedLoginAttempts = 5)
    {
        return new AuthService(
            userRepository,
            refreshTokenRepository,
            new TestPasswordHasher(),
            new TestAccessTokenProvider(),
            new TestRefreshTokenProvider(),
            new NoOpApplicationTelemetry(),
            Options.Create(new AuthenticationSecurityOptions
            {
                MaxFailedLoginAttempts = maxFailedLoginAttempts,
                LockoutMinutes = 15
            }));
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

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"hashed-{password}";
        }

        public bool Verify(string password, string passwordHash)
        {
            return Hash(password) == passwordHash;
        }
    }

    private sealed class TestAccessTokenProvider : IAccessTokenProvider
    {
        public AccessToken Generate(User user)
        {
            return new AccessToken("access-token", DateTime.UtcNow.AddMinutes(5));
        }
    }

    private sealed class TestRefreshTokenProvider : IRefreshTokenProvider
    {
        public string Generate()
        {
            return "refresh-token";
        }

        public string Hash(string refreshToken)
        {
            return $"hashed-{refreshToken}";
        }

        public DateTime GetExpirationUtc()
        {
            return DateTime.UtcNow.AddDays(7);
        }
    }
}
