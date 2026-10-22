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
}
