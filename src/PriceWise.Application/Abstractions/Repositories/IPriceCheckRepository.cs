using PriceWise.Application.PriceChecks;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IPriceCheckRepository
{
    Task<IReadOnlyCollection<PriceCheckCandidate>> ListCandidatesAsync(
        int maxProducts,
        CancellationToken cancellationToken = default);

    Task AddExecutionAsync(
        PriceCheckExecution execution,
        CancellationToken cancellationToken = default);

    Task<PriceCheckExecution?> GetLastExecutionAsync(
        CancellationToken cancellationToken = default);
}
