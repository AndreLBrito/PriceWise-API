using PriceWise.Api.Features.AlertNotifications;
using PriceWise.Api.Features.Authentication;
using PriceWise.Api.Features.Dashboard;
using PriceWise.Api.Features.Health;
using PriceWise.Api.Features.PriceAlerts;
using PriceWise.Api.Features.PriceHistories;
using PriceWise.Api.Features.Products;
using PriceWise.Api.Features.Stores;
using PriceWise.Api.Features.System;

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
        app.MapAlertNotificationEndpoints();
        app.MapDashboardEndpoints();
        app.MapSystemEndpoints();

        return app;
    }
}
