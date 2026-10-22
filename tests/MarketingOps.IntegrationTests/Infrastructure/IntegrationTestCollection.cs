namespace MarketingOps.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestContext>
{
    public const string Name = "Integration";
}
