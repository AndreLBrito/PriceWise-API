using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PriceWise.Api.Extensions;
using PriceWise.Api.Telemetry;
using PriceWise.Application.Abstractions.Telemetry;

namespace PriceWise.Tests.Unit.Telemetry;

public sealed class ApplicationTelemetryTests
{
    [Fact]
    public void AddPriceWiseOpenTelemetryRegistersTelemetryOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telemetry:Enabled"] = "false",
                ["Telemetry:ServiceName"] = "PriceWise.Tests",
                ["Telemetry:ServiceVersion"] = "9.9.9",
                ["Telemetry:Exporter"] = "Console",
                ["Telemetry:EnableMetrics"] = "true",
                ["Telemetry:EnableTracing"] = "false"
            })
            .Build();

        services.AddPriceWiseOpenTelemetry(configuration, new TestHostEnvironment());
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<TelemetryOptions>>().Value;

        options.Enabled.Should().BeFalse();
        options.ServiceName.Should().Be("PriceWise.Tests");
        options.ServiceVersion.Should().Be("9.9.9");
        options.EnableMetrics.Should().BeTrue();
        options.EnableTracing.Should().BeFalse();
    }

    [Fact]
    public void ApplicationTelemetryCreatesActivitySource()
    {
        using var telemetry = new ApplicationTelemetry();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = telemetry.StartActivity("Test.Activity");

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("Test.Activity");
        telemetry.ActivitySource.Name.Should().Be(TelemetryConstants.ActivitySourceName);
    }

    [Fact]
    public void ApplicationTelemetryRecordsCustomMetrics()
    {
        using var telemetry = new ApplicationTelemetry();
        var recordedValue = 0L;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == TelemetryConstants.MeterName
                && instrument.Name == TelemetryConstants.ProductsCreatedTotal)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => recordedValue += measurement);
        listener.Start();

        telemetry.RecordProductCreated();
        listener.RecordObservableInstruments();

        recordedValue.Should().Be(1);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "PriceWise.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
