using PriceWise.Application.Abstractions.Repositories;
using PriceWise.Application.Common;
using PriceWise.Application.PriceAlerts.Dtos;
using PriceWise.Domain.Entities;

namespace PriceWise.Application.PriceAlerts;

public sealed class PriceAlertService : IPriceAlertService
{
    private readonly IPriceAlertRepository priceAlertRepository;
    private readonly IProductRepository productRepository;

    public PriceAlertService(
        IPriceAlertRepository priceAlertRepository,
        IProductRepository productRepository)
    {
        this.priceAlertRepository = priceAlertRepository;
        this.productRepository = productRepository;
    }

    public async Task<Result<PriceAlertResponse>> CreateAsync(
        Guid userId,
        CreatePriceAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, userId, cancellationToken);

        if (product is null)
        {
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.ProductNotFound);
        }

        var existingAlert = await priceAlertRepository.GetActiveByProductIdAsync(
            userId,
            request.ProductId,
            cancellationToken);

        if (existingAlert is not null)
        {
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.ActiveAlertAlreadyExists);
        }

        var priceAlert = PriceAlert.Create(userId, request.ProductId, request.TargetPrice);

        await priceAlertRepository.AddAsync(priceAlert, cancellationToken);

        return Result<PriceAlertResponse>.Success(MapToResponse(priceAlert));
    }

    public async Task<Result<IReadOnlyCollection<PriceAlertResponse>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var priceAlerts = await priceAlertRepository.ListByUserIdAsync(userId, cancellationToken);
        var response = priceAlerts.Select(MapToResponse).ToArray();

        return Result<IReadOnlyCollection<PriceAlertResponse>>.Success(response);
    }

    public async Task<Result<PriceAlertResponse>> GetByIdAsync(
        Guid userId,
        Guid priceAlertId,
        CancellationToken cancellationToken = default)
    {
        var priceAlert = await priceAlertRepository.GetByIdAsync(priceAlertId, userId, cancellationToken);

        return priceAlert is null
            ? Result<PriceAlertResponse>.Failure(PriceAlertErrors.PriceAlertNotFound)
            : Result<PriceAlertResponse>.Success(MapToResponse(priceAlert));
    }

    public async Task<Result<PriceAlertResponse>> UpdateAsync(
        Guid userId,
        Guid priceAlertId,
        UpdatePriceAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        var priceAlert = await priceAlertRepository.GetByIdAsync(priceAlertId, userId, cancellationToken);

        if (priceAlert is null)
        {
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.PriceAlertNotFound);
        }

        var product = await productRepository.GetByIdAsync(priceAlert.ProductId, userId, cancellationToken);

        if (product is null)
        {
            return Result<PriceAlertResponse>.Failure(PriceAlertErrors.ProductNotFound);
        }

        priceAlert.Update(request.TargetPrice);

        await priceAlertRepository.UpdateAsync(priceAlert, cancellationToken);

        return Result<PriceAlertResponse>.Success(MapToResponse(priceAlert));
    }

    public async Task<Result> DeleteAsync(
        Guid userId,
        Guid priceAlertId,
        CancellationToken cancellationToken = default)
    {
        var priceAlert = await priceAlertRepository.GetByIdAsync(priceAlertId, userId, cancellationToken);

        if (priceAlert is null)
        {
            return Result.Failure(PriceAlertErrors.PriceAlertNotFound);
        }

        priceAlert.Deactivate();
        await priceAlertRepository.UpdateAsync(priceAlert, cancellationToken);

        return Result.Success();
    }

    private static PriceAlertResponse MapToResponse(PriceAlert priceAlert)
    {
        return new PriceAlertResponse(
            priceAlert.Id,
            priceAlert.ProductId,
            priceAlert.TargetPrice,
            priceAlert.IsActive,
            priceAlert.CreatedAtUtc,
            priceAlert.UpdatedAtUtc);
    }
}
