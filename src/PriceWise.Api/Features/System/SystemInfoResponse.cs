namespace PriceWise.Api.Features.System;

public sealed record SystemInfoResponse(
    string ApplicationName,
    string Version,
    string Environment,
    DateTime UtcNow);
