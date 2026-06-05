namespace PriceWise.Api.Extensions;

public static class ApiVersioningExtensions
{
    public const string CurrentVersionPrefix = "/api/v1";

    public static IApplicationBuilder UseApiVersionPrefix(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(CurrentVersionPrefix, out var remainingPath))
            {
                context.Request.Path = $"/api{remainingPath}";
            }

            await next(context);
        });
    }
}
