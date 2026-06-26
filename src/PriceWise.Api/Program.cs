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
    builder.Services.AddPriceWiseCors(builder.Configuration);
    builder.Services.AddApiAuthentication(builder.Configuration);
    builder.Services.AddApiAuthorization();
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
    app.UseStatusCodePages(async statusCodeContext =>
    {
        var httpContext = statusCodeContext.HttpContext;

        if (httpContext.Response.HasStarted || httpContext.Response.ContentLength is not null)
        {
            return;
        }

        var (code, message) = httpContext.Response.StatusCode switch
        {
            StatusCodes.Status404NotFound => ("Resource.NotFound", "Recurso não encontrado."),
            StatusCodes.Status401Unauthorized => ("Auth.Unauthorized", "Usuário não autenticado."),
            StatusCodes.Status403Forbidden => ("Auth.Forbidden", "Você não possui permissão para acessar este recurso."),
            _ => ("Http.Error", "Não foi possível concluir a requisição.")
        };

        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(PriceWise.Api.Common.ApiResponse<object>.Fail(
            code,
            message,
            httpContext.Response.StatusCode));
    });
    app.UseMiddleware<PriceWise.Api.Telemetry.CorrelationIdMiddleware>();
    app.UseApiVersionPrefix();
    if (app.Environment.IsDevelopment())
    {
        app.UseCors(PriceWise.Api.Cors.ApiCorsPolicyNames.Development);
    }

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
