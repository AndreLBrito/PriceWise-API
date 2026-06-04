using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Domain.Entities;

namespace PriceWise.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id as UserId, token_hash as TokenHash, expires_at_utc as ExpiresAtUtc,
                   revoked_at_utc as RevokedAtUtc, created_at_utc as CreatedAtUtc
            from refresh_tokens
            where token_hash = @TokenHash
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));

        return row?.ToRefreshToken();
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into refresh_tokens (id, user_id, token_hash, expires_at_utc, revoked_at_utc, created_at_utc)
            values (@Id, @UserId, @TokenHash, @ExpiresAtUtc, @RevokedAtUtc, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, refreshToken, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update refresh_tokens
            set revoked_at_utc = @RevokedAtUtc,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, refreshToken, cancellationToken: cancellationToken));
    }

    public async Task RevokeActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update refresh_tokens
            set revoked_at_utc = @RevokedAtUtc,
                updated_at_utc = @RevokedAtUtc
            where user_id = @UserId
              and revoked_at_utc is null
              and expires_at_utc > @RevokedAtUtc
            """;

        var revokedAtUtc = DateTime.UtcNow;
        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, RevokedAtUtc = revokedAtUtc },
            cancellationToken: cancellationToken));
    }

    private sealed record RefreshTokenRow(
        Guid Id,
        Guid UserId,
        string TokenHash,
        DateTime ExpiresAtUtc,
        DateTime? RevokedAtUtc,
        DateTime CreatedAtUtc)
    {
        public RefreshToken ToRefreshToken()
        {
            return RefreshToken.Restore(Id, UserId, TokenHash, ExpiresAtUtc, RevokedAtUtc, CreatedAtUtc);
        }
    }
}
