using PriceWise.Api.Features.Authentication;
using PriceWise.Api.Features.Health;
using PriceWise.Api.Features.System;

namespace PriceWise.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthEndpoints();
        app.MapAuthenticationEndpoints();
        app.MapSystemEndpoints();

        return app;
    }
}
