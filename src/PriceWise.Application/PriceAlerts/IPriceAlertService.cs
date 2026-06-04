using PriceWise.Application.Abstractions.Services;
using PriceWise.Application.Common;
using PriceWise.Application.PriceAlerts.Dtos;

namespace PriceWise.Application.PriceAlerts;

public interface IPriceAlertService : IService
{
    Task<Result<PriceAlertResponse>> CreateAsync(
        Guid userId,
        CreatePriceAlertRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<PriceAlertResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<PriceAlertResponse>> GetByIdAsync(
        Guid userId,
        Guid priceAlertId,
        CancellationToken cancellationToken = default);

    Task<Result<PriceAlertResponse>> UpdateAsync(
        Guid userId,
        Guid priceAlertId,
        UpdatePriceAlertRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid userId,
        Guid priceAlertId,
        CancellationToken cancellationToken = default);
}
