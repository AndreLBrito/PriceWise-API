using Dapper;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Domain.Entities;

namespace PriceWise.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, name, email, password_hash as PasswordHash, role, is_active as IsActive,
                   failed_login_attempts as FailedLoginAttempts, locked_until_utc as LockedUntilUtc,
                   created_at_utc as CreatedAtUtc
            from users
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToUser();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, name, email, password_hash as PasswordHash, role, is_active as IsActive,
                   failed_login_attempts as FailedLoginAttempts, locked_until_utc as LockedUntilUtc,
                   created_at_utc as CreatedAtUtc
            from users
            where email = @Email
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));

        return row?.ToUser();
    }

    public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into users (
                id, name, email, password_hash, role, is_active,
                failed_login_attempts, locked_until_utc, created_at_utc)
            values (
                @Id, @Name, @Email, @PasswordHash, @Role, @IsActive,
                @FailedLoginAttempts, @LockedUntilUtc, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(entity), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update users
            set name = @Name,
                email = @Email,
                password_hash = @PasswordHash,
                role = @Role,
                is_active = @IsActive,
                failed_login_attempts = @FailedLoginAttempts,
                locked_until_utc = @LockedUntilUtc,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(entity), cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from users where id = @Id";

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<User>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, name, email, password_hash as PasswordHash, role, is_active as IsActive,
                   failed_login_attempts as FailedLoginAttempts, locked_until_utc as LockedUntilUtc,
                   created_at_utc as CreatedAtUtc
            from users
            order by created_at_utc desc
            limit @PageSize offset @Offset
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<UserRow>(
            new CommandDefinition(
                sql,
                new { PageSize = pageSize, Offset = (page - 1) * pageSize },
                cancellationToken: cancellationToken));

        return rows.Select(row => row.ToUser()).ToArray();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "select count(*) from users";

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static object ToParameters(User user)
    {
        return new
        {
            user.Id,
            user.Name,
            user.Email,
            user.PasswordHash,
            Role = user.Role.ToString(),
            user.IsActive,
            user.FailedLoginAttempts,
            user.LockedUntilUtc,
            user.CreatedAtUtc,
            user.UpdatedAtUtc
        };
    }

    private sealed record UserRow(
        Guid Id,
        string Name,
        string Email,
        string PasswordHash,
        string Role,
        bool IsActive,
        int FailedLoginAttempts,
        DateTime? LockedUntilUtc,
        DateTime CreatedAtUtc)
    {
        public User ToUser()
        {
            var role = Enum.TryParse<PriceWise.Domain.Entities.UserRole>(Role, true, out var parsedRole)
                ? parsedRole
                : PriceWise.Domain.Entities.UserRole.User;

            return User.Restore(
                Id,
                Name,
                Email,
                PasswordHash,
                role,
                IsActive,
                FailedLoginAttempts,
                LockedUntilUtc,
                CreatedAtUtc);
        }
    }
}
