using Hangfire;
using MarketingOps.Application;
using MarketingOps.Infrastructure;
using MarketingOps.Infrastructure.Jobs;

// Arka plan işlerini (taramalar, zamanlanmış işler) işleyen süreç. Web yalnızca iş ekler; işleyen burasıdır.
var builder = Host.CreateApplicationBuilder(args);

// Tek yapılandırma dosyası Web projesindeki appsettings.json'dır; derlemede bu projenin çıktısına kopyalanır.
// Yerelde dotnet run içerik kökünü proje klasörü yaptığı için dosya çıktı klasöründen ayrıca okunur.
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireServer(options =>
{
    options.Queues = ["crawls", "default"];
    // Her tarama kendi içinde paralel istek atar; aynı anda çok sayıda tarama siteleri ve işçiyi yorar.
    options.WorkerCount = 4;
});
builder.Services.AddHostedService<RecurringCrawlSync>();

builder.Build().Run();
