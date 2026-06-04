using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.PriceChecks.Dtos;

namespace PriceWise.Application.PriceChecks;

public interface IPriceCheckService : IService
{
    Task<Result<PriceCheckRunResponse>> RunAsync(CancellationToken cancellationToken = default);

    Task<Result<PriceCheckStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default);
}
