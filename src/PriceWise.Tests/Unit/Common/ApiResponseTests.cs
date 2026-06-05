using FluentAssertions;
using PriceWise.Api.Common;
using PriceWise.Api.Telemetry;

namespace PriceWise.Tests.Unit.Common;

public sealed class ApiResponseTests
{
    [Fact]
    public void FailIncludesCorrelationId()
    {
        CorrelationContext.CorrelationId = "correlation-test";

        var response = ApiResponse<object>.Fail(
            "Validation.InvalidRequest",
            "Dados inválidos.",
            400);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.CorrelationId.Should().Be("correlation-test");
        response.Error.StatusCode.Should().Be(400);

        CorrelationContext.CorrelationId = null;
    }
}
