using FluentAssertions;
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
    }

    private static AuthService CreateService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        return new AuthService(
            userRepository,
            refreshTokenRepository,
            new TestPasswordHasher(),
            new TestAccessTokenProvider(),
            new TestRefreshTokenProvider(),
            new NoOpApplicationTelemetry());
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
