using System.Diagnostics;

namespace PriceWise.Application.Abstractions.Telemetry;

public sealed class NoOpApplicationTelemetry : IApplicationTelemetry
{
    public ActivitySource ActivitySource { get; } = new(TelemetryConstants.ActivitySourceName);

    public Activity? StartActivity(string name) => null;

    public void RecordProductCreated()
    {
    }

    public void RecordStoreCreated()
    {
    }

    public void RecordPriceHistoryCreated()
    {
    }

    public void RecordPriceAlertCreated()
    {
    }

    public void RecordAlertNotificationCreated()
    {
    }

    public void RecordPriceCheck(PriceCheckTrigger trigger)
    {
    }

    public void RecordError(Exception exception)
    {
    }

    public void RecordError(string errorCode)
    {
    }
}
