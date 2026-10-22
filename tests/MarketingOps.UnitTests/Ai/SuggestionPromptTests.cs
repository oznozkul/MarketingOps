using MarketingOps.Application.Tasks.Suggestions;
using MarketingOps.Domain.WorkTasks;
using MarketingOps.Infrastructure.Ai;

namespace MarketingOps.UnitTests.Ai;

public class SuggestionPromptTests
{
    private static AiSuggestionRequest Request(SuggestionKind kind = SuggestionKind.Text) => new(
        "seo.title.duplicate", "Title tekrarı", "Aynı title'ı paylaşan sayfaları bulur.", "google:title-links", "Ebat", 14,
        "Ebat sayfalarından 14 tanesi etkileniyor.", "Etki", kind, "Özet", "Kalıp",
        [new SuggestionExample("https://ornek.com/a", "Aynı", null), new SuggestionExample("https://ornek.com/b", "Aynı", null)]);

    [Fact]
    public void Prompt_contains_only_given_data_and_knowledge()
    {
        var prompt = SuggestionPrompt.User(Request(), ["Title 60 karakteri geçmemeli."]);
        prompt.ShouldContain("Etkilenen sayfa sayısı: 14");
        prompt.ShouldContain("https://ornek.com/a | Aynı");
        prompt.ShouldContain("Bilgi tabanı (google:title-links):");
        prompt.ShouldContain("Title 60 karakteri geçmemeli.");
        SuggestionPrompt.System.ShouldContain("Verilmeyen hiçbir sayı");
    }

    [Fact]
    public void Suggested_values_for_unknown_urls_are_dropped()
    {
        var result = SuggestionPrompt.Parse("""
            {"impact":"Neden önemli.","summary":"Özet","content":"İçerik",
             "suggestedValues":[{"url":"https://ornek.com/a","suggested":" 185/65 R15 Yaz Lastiği | Marka "},{"url":"https://uydurma.com/x","suggested":"x"}]}
            """, Request());

        result.Content.ShouldBe("İçerik");
        result.SuggestedValues.ShouldHaveSingleItem().ShouldBe(new AiSuggestedValue("https://ornek.com/a", "185/65 R15 Yaz Lastiği | Marka"));
    }

    [Fact]
    public void Empty_content_is_rejected()
    {
        Should.Throw<InvalidOperationException>(() => SuggestionPrompt.Parse("""{"impact":"x","summary":"y","content":"","suggestedValues":[]}""", Request()));
    }
}
