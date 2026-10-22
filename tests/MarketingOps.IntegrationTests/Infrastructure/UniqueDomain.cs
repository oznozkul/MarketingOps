namespace MarketingOps.IntegrationTests.Infrastructure;

/// <summary>Testler aynı veritabanını paylaştığından her test kendi domain'ini kullanır.</summary>
public static class UniqueDomain
{
    public static string Next() => $"t{Guid.NewGuid():N}"[..13] + ".example.com";
}
