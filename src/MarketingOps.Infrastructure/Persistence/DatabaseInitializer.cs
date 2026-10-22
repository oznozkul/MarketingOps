using MarketingOps.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Yalnızca Development'ta çağrılır: bekleyen migration'ları uygular ve örnek veriyi ekler.</summary>
    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
        await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync(ct);
    }
}
