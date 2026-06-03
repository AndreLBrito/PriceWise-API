using PriceWise.Domain.Entities;

namespace PriceWise.Application.Abstractions.Auth;

public interface IAccessTokenProvider
{
    AccessToken Generate(User user);
}
