using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PriceWise.Api.Common;
using PriceWise.Api.RateLimiting;

namespace PriceWise.Api.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddPriceWiseRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = ReadOptions(configuration);

        services.Configure<RateLimitOptions>(rateLimitOptions =>
        {
            rateLimitOptions.Enabled = options.Enabled;
            rateLimitOptions.LoginPermitLimit = options.LoginPermitLimit;
            rateLimitOptions.LoginWindowInMinutes = options.LoginWindowInMinutes;
            rateLimitOptions.RefreshTokenPermitLimit = options.RefreshTokenPermitLimit;
            rateLimitOptions.RefreshTokenWindowInMinutes = options.RefreshTokenWindowInMinutes;
            rateLimitOptions.GeneralPermitLimit = options.GeneralPermitLimit;
            rateLimitOptions.GeneralWindowInMinutes = options.GeneralWindowInMinutes;
            rateLimitOptions.PriceCheckPermitLimit = options.PriceCheckPermitLimit;
            rateLimitOptions.PriceCheckWindowInMinutes = options.PriceCheckWindowInMinutes;
        });

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("PriceWise.RateLimiting");
                var partitionKey = GetPartitionKey(context.HttpContext);

                logger.LogWarning(
                    "Requisição bloqueada por limite excedido. Policy: {RateLimitPolicy}, PartitionKey: {PartitionKey}, Path: {Path}",
                    context.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reason) ? reason : "unknown",
                    partitionKey,
                    context.HttpContext.Request.Path.Value);

                var response = ApiResponse<object>.Fail(
                    "RateLimiting.TooManyRequests",
                    "Limite de requisições excedido. Tente novamente em instantes.");

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };

            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.Login,
                httpContext =>
                {
                    var currentOptions = ReadOptions(httpContext.RequestServices.GetRequiredService<IConfiguration>());
                    return CreatePartition(
                        httpContext,
                        currentOptions.Enabled,
                        currentOptions.LoginPermitLimit,
                        currentOptions.LoginWindowInMinutes,
                        RateLimitPolicyNames.Login);
                });

            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.RefreshToken,
                httpContext =>
                {
                    var currentOptions = ReadOptions(httpContext.RequestServices.GetRequiredService<IConfiguration>());
                    return CreatePartition(
                        httpContext,
                        currentOptions.Enabled,
                        currentOptions.RefreshTokenPermitLimit,
                        currentOptions.RefreshTokenWindowInMinutes,
                        RateLimitPolicyNames.RefreshToken);
                });

            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.General,
                httpContext =>
                {
                    var currentOptions = ReadOptions(httpContext.RequestServices.GetRequiredService<IConfiguration>());
                    return CreatePartition(
                        httpContext,
                        currentOptions.Enabled,
                        currentOptions.GeneralPermitLimit,
                        currentOptions.GeneralWindowInMinutes,
                        RateLimitPolicyNames.General);
                });

            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.PriceCheck,
                httpContext =>
                {
                    var currentOptions = ReadOptions(httpContext.RequestServices.GetRequiredService<IConfiguration>());
                    return CreatePartition(
                        httpContext,
                        currentOptions.Enabled,
                        currentOptions.PriceCheckPermitLimit,
                        currentOptions.PriceCheckWindowInMinutes,
                        RateLimitPolicyNames.PriceCheck);
                });
        });

        return services;
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext httpContext,
        bool enabled,
        int permitLimit,
        int windowInMinutes,
        string policyName)
    {
        var partitionKey = $"{policyName}:{GetPartitionKey(httpContext)}";

        if (!enabled)
        {
            return RateLimitPartition.GetNoLimiter(partitionKey);
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, permitLimit),
                Window = TimeSpan.FromMinutes(Math.Max(1, windowInMinutes)),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    private static string GetPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private static RateLimitOptions ReadOptions(IConfiguration configuration)
    {
        return new RateLimitOptions
        {
            Enabled = ReadBool(configuration, $"{RateLimitOptions.SectionName}:Enabled", true),
            LoginPermitLimit = ReadInt(configuration, $"{RateLimitOptions.SectionName}:LoginPermitLimit", 5),
            LoginWindowInMinutes = ReadInt(configuration, $"{RateLimitOptions.SectionName}:LoginWindowInMinutes", 1),
            RefreshTokenPermitLimit = ReadInt(configuration, $"{RateLimitOptions.SectionName}:RefreshTokenPermitLimit", 10),
            RefreshTokenWindowInMinutes = ReadInt(configuration, $"{RateLimitOptions.SectionName}:RefreshTokenWindowInMinutes", 1),
            GeneralPermitLimit = ReadInt(configuration, $"{RateLimitOptions.SectionName}:GeneralPermitLimit", 100),
            GeneralWindowInMinutes = ReadInt(configuration, $"{RateLimitOptions.SectionName}:GeneralWindowInMinutes", 1),
            PriceCheckPermitLimit = ReadInt(configuration, $"{RateLimitOptions.SectionName}:PriceCheckPermitLimit", 3),
            PriceCheckWindowInMinutes = ReadInt(configuration, $"{RateLimitOptions.SectionName}:PriceCheckWindowInMinutes", 5)
        };
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool defaultValue)
    {
        return bool.TryParse(configuration[key], out var value) ? value : defaultValue;
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
    {
        return int.TryParse(configuration[key], out var value) ? value : defaultValue;
    }
}
