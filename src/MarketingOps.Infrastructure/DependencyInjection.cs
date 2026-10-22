using System.Net;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.DataProtection;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Infrastructure.Ai;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.Infrastructure.Jobs;
using MarketingOps.Infrastructure.Persistence;
using MarketingOps.Infrastructure.Persistence.Seed;
using MarketingOps.Infrastructure.Tracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    /// <summary>Web ve Worker'ın ortak altyapısı. Hangfire istemcisi buradadır; iş işleyen sunucu yalnızca Worker'da eklenir.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} tanımlı değil.");

        // Site başına sorgulanan koleksiyonlar küçük; tek sorgu (SingleQuery) yeterli ve bilinçli seçim.
        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton(TimeProvider.System);

        // Konteyner yeniden oluşturulduğunda anahtarlar kaybolursa antiforgery çerezleri ve şifreli connector kimlik bilgileri
        // (Jira API anahtarı) çözülemez. Web ve Worker aynı anahtar klasörünü kullanır.
        var dataProtection = services.AddDataProtection().SetApplicationName("MarketingOps");
        if (configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        services.AddScoped<DevelopmentSeeder>();

        // Türkçe sitelerde windows-1254 gibi kod sayfaları görülebiliyor.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        services.AddHttpClient<ICrawlerHttpClient, CrawlerHttpClient>(client => client.Timeout = TimeSpan.FromSeconds(20))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });
        services.AddSingleton<SiteCrawler>();
        services.AddTransient<CrawlJob>();
        services.AddTransient<ScheduledCrawlJob>();
        services.AddTransient<EvaluateCrawlJob>();
        services.AddTransient<EnrichTaskJob>();
        services.AddSingleton<ITaskJobScheduler, HangfireTaskJobScheduler>();
        services.AddHttpClient<ITaskTracker, JiraTaskTracker>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddAiSuggestions(configuration);

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(connectionString)));
        services.AddSingleton<ICrawlScheduler, HangfireCrawlScheduler>();
        return services;
    }
}
