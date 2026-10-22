using MarketingOps.Domain.Rules;
using MarketingOps.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketingOps.UnitTests.Ai;

public class KnowledgeBaseTests
{
    [Fact]
    public void Chunks_are_selected_by_reference_key()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "perf.md"), """
                # Web Sitesi Performans Optimizasyonu
                <!-- ref: perf-doc:p30 -->
                Görsellere width ve height verin.
                <!-- ref: perf-doc:p32 -->
                Ekran altı görselleri tembel yükleyin.
                """);
            File.WriteAllText(Path.Combine(dir.FullName, "README.md"), "<!-- ref: perf-doc:p30 -->\nBiçim örneği; bilgi değildir.");

            var kb = new KnowledgeBase(dir.FullName, NullLogger<KnowledgeBase>.Instance);
            kb.For("perf-doc:p30").ShouldBe(["Görsellere width ve height verin."]);
            kb.For("PERF-DOC:P32").ShouldHaveSingleItem().ShouldContain("tembel");
            kb.For("google:links").ShouldBeEmpty();
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public void Missing_folder_is_not_an_error() =>
        new KnowledgeBase("/yok/boyle/klasor", NullLogger<KnowledgeBase>.Instance).For("perf-doc:p30").ShouldBeEmpty();

    [Fact]
    public void Every_rule_reference_has_a_knowledge_chunk()
    {
        var kb = new KnowledgeBase(Path.Combine(RepoRoot(), "knowledge"), NullLogger<KnowledgeBase>.Instance);
        var missing = RuleCatalog.All.Select(r => r.ReferenceKey).Distinct().Where(key => kb.For(key).Count == 0).ToList();
        missing.ShouldBeEmpty();
    }

    /// <summary>Test çıktısı bin altındadır; depo kökü knowledge/README.md ile bulunur.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "knowledge", "README.md"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("knowledge klasörü bulunamadı.");
    }
}
