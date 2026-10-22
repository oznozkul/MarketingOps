using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MarketingOps.IntegrationTests.Infrastructure;

/// <summary>
/// Uygulamayı Development ortamında, test konteynerine bağlı olarak başlatır. Development olduğu için
/// başlangıçta migration'lar ve örnek veri uygulanır.
/// </summary>
public sealed class MarketingOpsWebFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
    }
}
