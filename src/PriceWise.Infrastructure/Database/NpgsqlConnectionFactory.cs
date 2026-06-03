using System.Data;
using Npgsql;
using PriceWise.Application.Abstractions.Data;

namespace PriceWise.Infrastructure.Database;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource dataSource;

    public NpgsqlConnectionFactory(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await dataSource.OpenConnectionAsync(cancellationToken);
    }
}
