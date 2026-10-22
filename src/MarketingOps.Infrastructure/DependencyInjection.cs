using System.Net;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.DataProtection;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors;
using MarketingOps.Infrastructure.Ai;
using MarketingOps.Infrastructure.Google;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.Application.Notifications;
using MarketingOps.Application.Tracking;
using MarketingOps.Infrastructure.Jobs;
using MarketingOps.Infrastructure.Notifications;
using MarketingOps.Infrastructure.Persistence;
using MarketingOps.Infrastructure.Tracking;
using MarketingOps.Infrastructure.Persistence.Seed;
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
        // (Google refresh token) çözülemez. Web ve Worker aynı anahtar klasörünü kullanır.
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
        services.AddTransient<SnapshotRetentionJob>();
        services.AddSingleton<ITaskJobScheduler, HangfireTaskJobScheduler>();
        services.AddAiSuggestions(configuration);
        AddGoogle(services, configuration);

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            // Taramalar saatlerce sürebilir. Kayan görünmezlik süresi açık değilse 30. dakikada iş "sahipsiz" sayılır ve
            // aynı tarama ikinci bir işçide de başlar. Açıkken çalışan iş canlı tutulur; işçi ölürse 5 dakikada başka işçiye geçer.
            .UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                UseSlidingInvisibilityTimeout = true,
                InvisibilityTimeout = TimeSpan.FromMinutes(5)
            }));
        services.AddSingleton<ICrawlScheduler, HangfireCrawlScheduler>();
        services.AddSingleton<ITrackingBrowser, PlaywrightTrackingBrowser>();
        services.AddSingleton<IOrderSource, SqlServerOrderSource>();
        services.AddHttpClient<IMetaGraphClient, MetaGraphClient>();
        services.AddTransient<TrackingAuditJob>();
        services.AddSingleton<ITrackingJobScheduler, HangfireTrackingScheduler>();

        services.AddSingleton(configuration.GetSection(MailOptions.Section).Get<MailOptions>() ?? new MailOptions());
        services.AddHttpClient("webhooks", client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddSingleton<ISummaryReportRenderer, SummaryReportHtml>();
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<INotificationChannel, TeamsNotificationChannel>();
        services.AddScoped<INotificationChannel, SlackNotificationChannel>();
        services.AddScoped<INotificationChannel, InAppNotificationChannel>();
        services.AddSingleton<INotificationJobScheduler, HangfireNotificationScheduler>();
        services.AddTransient<NotificationDispatchJob>();
        services.AddTransient<SendDueReportsJob>();
        return services;
    }

    /// <summary>Google istemcileri: kota (429) ve geçici hatalar için standart yeniden deneme ve zaman aşımı.</summary>
    private static void AddGoogle(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(GoogleOptions.Section).Get<GoogleOptions>() ?? new GoogleOptions();
        services.AddSingleton(options);
        services.AddSingleton(configuration.GetSection(GoogleOptions.Section).Get<GoogleSyncOptions>() ?? new GoogleSyncOptions());
        services.AddTransient<ConnectorSyncJob>();
        services.AddHttpClient<IGoogleAuthClient, GoogleAuthClient>().AddStandardResilienceHandler();
        services.AddHttpClient<ISearchConsoleClient, SearchConsoleClient>().AddStandardResilienceHandler();
        services.AddHttpClient<IGa4Client, Ga4Client>().AddStandardResilienceHandler();
        // PageSpeed bir ölçümü 30–60 sn'de tamamlayabilir.
        services.AddHttpClient<IPageSpeedClient, PageSpeedClient>().AddStandardResilienceHandler(o =>
        {
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(90);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(4);
            o.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(3);
        });
    }
}
