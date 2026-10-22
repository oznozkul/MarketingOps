using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarketingOps.IntegrationTests.Infrastructure;

/// <summary>Konteyner ve uygulama tüm testler boyunca bir kez başlatılır.</summary>
public sealed class IntegrationTestContext : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = new();

    public MarketingOpsWebFactory Factory { get; private set; } = null!;

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();
        Factory = new MarketingOpsWebFactory(_postgres.ConnectionString);
        _ = Factory.Server; // Uygulamayı (ve migration + seed'i) şimdi başlat.
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
