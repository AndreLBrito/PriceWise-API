using PriceWise.Application.Common;

namespace PriceWise.Application.Abstractions.DataSeeding;

public interface IDataSeeder
{
    Task<Result> SeedAsync(CancellationToken cancellationToken = default);
}
