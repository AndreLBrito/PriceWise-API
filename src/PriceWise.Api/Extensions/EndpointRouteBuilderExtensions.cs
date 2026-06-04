using PriceWise.Api.Features.AlertNotifications;
using PriceWise.Api.Features.Authentication;
using PriceWise.Api.Features.Dashboard;
using PriceWise.Api.Features.Health;
using PriceWise.Api.Features.NotificationChannels;
using PriceWise.Api.Features.PriceAlerts;
using PriceWise.Api.Features.PriceChecks;
using PriceWise.Api.Features.PriceHistories;
using PriceWise.Api.Features.Products;
using PriceWise.Api.Features.Stores;
using PriceWise.Api.Features.System;
using PriceWise.Api.Features.Telemetry;

namespace PriceWise.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthEndpoints();
        app.MapAuthenticationEndpoints();
        app.MapProductEndpoints();
        app.MapStoreEndpoints();
        app.MapPriceHistoryEndpoints();
        app.MapPriceAlertEndpoints();
        app.MapNotificationChannelEndpoints();
        app.MapPriceCheckEndpoints();
        app.MapAlertNotificationEndpoints();
        app.MapDashboardEndpoints();
        app.MapSystemEndpoints();
        app.MapTelemetryEndpoints();

        return app;
    }
}
