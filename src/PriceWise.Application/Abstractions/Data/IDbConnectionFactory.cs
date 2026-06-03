using System.Data;

namespace PriceWise.Application.Abstractions.Data;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
