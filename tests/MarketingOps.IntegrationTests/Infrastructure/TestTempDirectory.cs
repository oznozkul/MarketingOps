using System.Runtime.CompilerServices;

namespace MarketingOps.IntegrationTests.Infrastructure;

/// <summary>
/// Bazı antivirüsler Playwright'ın node sürecinin %TEMP%'e yazmasını engelliyor (EPERM mkdtemp) ve render testleri
/// tarayıcıyı açamıyor. Testler kendi çıktı klasörü altındaki geçici klasörü kullanır; alt süreçler bunu devralır.
/// </summary>
internal static class TestTempDirectory
{
    [ModuleInitializer]
    internal static void Redirect()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "tmp");
        Directory.CreateDirectory(path);
        Environment.SetEnvironmentVariable("TEMP", path);
        Environment.SetEnvironmentVariable("TMP", path);
    }
}
