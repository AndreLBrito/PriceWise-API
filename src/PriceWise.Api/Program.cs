using PriceWise.Api.Extensions;
using PriceWise.Application.DependencyInjection;
using PriceWise.Infrastructure.DependencyInjection;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddOpenApi();
    builder.Services.AddApi();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();
    builder.Services.AddPriceWiseRateLimiting(builder.Configuration);
    builder.Services.AddPriceWiseOpenTelemetry(builder.Configuration, builder.Environment);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarDocs();
    }

    await app.UseDatabaseMigrationsAsync();
    await app.UseDemoDataSeedAsync();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseMiddleware<PriceWise.Api.Telemetry.CorrelationIdMiddleware>();
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapApiEndpoints();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "PriceWise API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
