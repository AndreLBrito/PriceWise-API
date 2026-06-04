using PriceWise.Api.Authorization;
using PriceWise.Domain.Entities;

namespace PriceWise.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicyNames.AuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AuthorizationPolicyNames.AdminOnly, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(AuthorizationPolicyNames.PriceCheckManagement, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(AuthorizationPolicyNames.SystemManagement, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(AuthorizationPolicyNames.TelemetryManagement, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(AuthorizationPolicyNames.SeedManagement, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(UserRole.Admin.ToString()));
        });

        return services;
    }
}
