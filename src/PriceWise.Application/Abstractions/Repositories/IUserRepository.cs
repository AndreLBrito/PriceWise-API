using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<User>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
