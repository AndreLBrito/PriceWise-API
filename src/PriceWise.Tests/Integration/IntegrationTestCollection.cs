namespace PriceWise.Tests.Integration;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<PriceWiseWebApplicationFactory>
{
    public const string Name = "Integration";
}
