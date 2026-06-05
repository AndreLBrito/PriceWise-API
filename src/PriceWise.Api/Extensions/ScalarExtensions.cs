using Scalar.AspNetCore;

namespace PriceWise.Api.Extensions;

public static class ScalarExtensions
{
    public static IEndpointRouteBuilder MapScalarDocs(this IEndpointRouteBuilder app)
    {
        app.MapScalarApiReference(options =>
        {
            options.Title = "PriceWise API v1";
            options.Theme = ScalarTheme.BluePlanet;
        });

        return app;
    }
}
