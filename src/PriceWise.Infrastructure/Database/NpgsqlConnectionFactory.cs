using System.Data;
using Npgsql;
using PriceWise.Application.Abstractions.Data;
using PriceWise.Application.Abstractions.Telemetry;

namespace PriceWise.Infrastructure.Database;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource dataSource;
    private readonly IApplicationTelemetry telemetry;

    public NpgsqlConnectionFactory(
        NpgsqlDataSource dataSource,
        IApplicationTelemetry telemetry)
    {
        this.dataSource = dataSource;
        this.telemetry = telemetry;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.StartActivity("Database.OpenConnection");
        return await dataSource.OpenConnectionAsync(cancellationToken);
    }
}
