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
            select id, name, email, password_hash as PasswordHash, is_active as IsActive, created_at_utc as CreatedAtUtc
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
            select id, name, email, password_hash as PasswordHash, is_active as IsActive, created_at_utc as CreatedAtUtc
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
            insert into users (id, name, email, password_hash, is_active, created_at_utc)
            values (@Id, @Name, @Email, @PasswordHash, @IsActive, @CreatedAtUtc)
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update users
            set name = @Name,
                email = @Email,
                password_hash = @PasswordHash,
                is_active = @IsActive,
                updated_at_utc = @UpdatedAtUtc
            where id = @Id
            """;

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from users where id = @Id";

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private sealed record UserRow(
        Guid Id,
        string Name,
        string Email,
        string PasswordHash,
        bool IsActive,
        DateTime CreatedAtUtc)
    {
        public User ToUser()
        {
            return User.Restore(Id, Name, Email, PasswordHash, IsActive, CreatedAtUtc);
        }
    }
}
