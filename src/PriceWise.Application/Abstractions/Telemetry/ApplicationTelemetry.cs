using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PriceWise.Application.Abstractions.Telemetry;

public sealed class ApplicationTelemetry : IApplicationTelemetry, IDisposable
{
    private readonly Meter meter;
    private readonly Counter<long> productsCreatedCounter;
    private readonly Counter<long> storesCreatedCounter;
    private readonly Counter<long> priceHistoriesCreatedCounter;
    private readonly Counter<long> priceAlertsCreatedCounter;
    private readonly Counter<long> alertNotificationsCreatedCounter;
    private readonly Counter<long> manualPriceChecksCounter;
    private readonly Counter<long> automaticPriceChecksCounter;

    public ApplicationTelemetry()
    {
        ActivitySource = new ActivitySource(TelemetryConstants.ActivitySourceName);
        meter = new Meter(TelemetryConstants.MeterName);
        productsCreatedCounter = meter.CreateCounter<long>(TelemetryConstants.ProductsCreatedTotal);
        storesCreatedCounter = meter.CreateCounter<long>(TelemetryConstants.StoresCreatedTotal);
        priceHistoriesCreatedCounter = meter.CreateCounter<long>(TelemetryConstants.PriceHistoriesCreatedTotal);
        priceAlertsCreatedCounter = meter.CreateCounter<long>(TelemetryConstants.PriceAlertsCreatedTotal);
        alertNotificationsCreatedCounter = meter.CreateCounter<long>(TelemetryConstants.AlertNotificationsCreatedTotal);
        manualPriceChecksCounter = meter.CreateCounter<long>(TelemetryConstants.ManualPriceChecksTotal);
        automaticPriceChecksCounter = meter.CreateCounter<long>(TelemetryConstants.AutomaticPriceChecksTotal);
    }

    public ActivitySource ActivitySource { get; }

    public Activity? StartActivity(string name)
    {
        return ActivitySource.StartActivity(name, ActivityKind.Internal);
    }

    public void RecordProductCreated() => productsCreatedCounter.Add(1);

    public void RecordStoreCreated() => storesCreatedCounter.Add(1);

    public void RecordPriceHistoryCreated() => priceHistoriesCreatedCounter.Add(1);

    public void RecordPriceAlertCreated() => priceAlertsCreatedCounter.Add(1);

    public void RecordAlertNotificationCreated() => alertNotificationsCreatedCounter.Add(1);

    public void RecordPriceCheck(PriceCheckTrigger trigger)
    {
        if (trigger == PriceCheckTrigger.Automatic)
        {
            automaticPriceChecksCounter.Add(1);
            return;
        }

        manualPriceChecksCounter.Add(1);
    }

    public void RecordError(Exception exception)
    {
        Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
        Activity.Current?.AddException(exception);
    }

    public void RecordError(string errorCode)
    {
        Activity.Current?.SetStatus(ActivityStatusCode.Error, errorCode);
        Activity.Current?.SetTag("error.code", errorCode);
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        meter.Dispose();
    }
}
