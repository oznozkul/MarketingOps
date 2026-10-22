using Testcontainers.PostgreSql;

namespace MarketingOps.IntegrationTests.Infrastructure;

/// <summary>Tüm entegrasyon testleri için tek bir Postgres 17 konteyneri.</summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
