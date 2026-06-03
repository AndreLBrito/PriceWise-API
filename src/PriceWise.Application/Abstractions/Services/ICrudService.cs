using PriceWise.Application.Common;

namespace PriceWise.Application.Abstractions.Services;

public interface ICrudService<TResponse, in TCreateRequest, in TUpdateRequest> : IService
{
    Task<Result<TResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<TResponse>> CreateAsync(TCreateRequest request, CancellationToken cancellationToken = default);

    Task<Result<TResponse>> UpdateAsync(Guid id, TUpdateRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
