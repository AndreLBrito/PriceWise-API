using System.Diagnostics;

namespace PriceWise.Application.Abstractions.Telemetry;

public interface IApplicationTelemetry
{
    ActivitySource ActivitySource { get; }

    Activity? StartActivity(string name);

    void RecordProductCreated();

    void RecordStoreCreated();

    void RecordPriceHistoryCreated();

    void RecordPriceAlertCreated();

    void RecordAlertNotificationCreated();

    void RecordPriceCheck(PriceCheckTrigger trigger);

    void RecordError(Exception exception);

    void RecordError(string errorCode);
}
