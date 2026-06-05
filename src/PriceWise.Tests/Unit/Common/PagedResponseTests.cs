using FluentAssertions;
using PriceWise.Application.Common;

namespace PriceWise.Tests.Unit.Common;

public sealed class PagedResponseTests
{
    [Fact]
    public void CreateCalculatesPaginationMetadata()
    {
        var response = PagedResponse<string>.Create(["A", "B"], 2, 2, 5);

        response.Page.Should().Be(2);
        response.PageSize.Should().Be(2);
        response.TotalItems.Should().Be(5);
        response.TotalPages.Should().Be(3);
        response.HasNextPage.Should().BeTrue();
        response.HasPreviousPage.Should().BeTrue();
        response.Items.Should().BeEquivalentTo(["A", "B"], options => options.WithStrictOrdering());
    }

    [Fact]
    public void ListRequestNormalizesInvalidPaginationValues()
    {
        var request = new ListRequest(Page: -1, PageSize: 500);

        request.NormalizedPage.Should().Be(1);
        request.NormalizedPageSize.Should().Be(100);
        request.Offset.Should().Be(0);
    }
}
