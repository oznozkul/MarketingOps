using System.Text;
using System.Text.Json;
using MarketingOps.Application.PageTypes.Suggestions;

namespace MarketingOps.Infrastructure.Ai;

/// <summary>Sayfa tipi önerisinin istemi ve çıktı şeması. Model yalnızca ad ve glob deseni önerir; kapsadığı sayfaları uygulama hesaplar.</summary>
public static class PageTypePrompt
{
    public const int MaxSuggestions = 12;

    public const string System =
        """
        Bir e-ticaret sitesinin denetim aracında, sitenin sayfalarını gruplayan "sayfa tipleri" öneriyorsun (ör. Ürün, Kategori, Marka, Blog yazısı, Statik sayfa).
        Kurallar:
        - Yalnızca verilen URL gruplarına ve örnek adreslere dayan; örneklerde görünmeyen bölüm uydurma.
        - Her öneri için Türkçe, kısa bir ad ve URL path'ine uygulanan bir glob deseni ver.
        - Glob: "*" tek bir path segmentiyle, "**" sıfır ya da daha fazla segmentle eşleşir; desen "/" ile başlar ve query string'i kapsamaz.
          Örnekler: "/urun/**", "/*.html", "/blog/*", "/marka/*".
        - Aynı sayfaları kapsayan iki öneri verme; mevcut sayfa tipleriyle aynı adı kullanma.
        - Ana sayfa ("/") için öneri verme.
        - reason alanında desenin neden bu sayfa grubuna uyduğunu tek cümleyle yaz.
        Yanıtın yalnızca istenen JSON nesnesi olsun.
        """;

    public static readonly JsonElement Schema = JsonSerializer.SerializeToElement(new Dictionary<string, object>
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = new[] { "pageTypes" },
        ["properties"] = new Dictionary<string, object>
        {
            ["pageTypes"] = new
            {
                type = "array",
                description = $"En fazla {MaxSuggestions} sayfa tipi önerisi, en çok sayfayı kapsayandan başlayarak.",
                items = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["required"] = new[] { "name", "urlGlob", "reason" },
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["name"] = new { type = "string" },
                        ["urlGlob"] = new { type = "string" },
                        ["reason"] = new { type = "string" }
                    }
                }
            }
        }
    });

    public static string User(PageTypeSuggestionRequest r)
    {
        var text = new StringBuilder()
            .AppendLine($"Site: {r.Domain}")
            .AppendLine(r.ExistingPageTypes.Count > 0 ? $"Mevcut sayfa tipleri: {string.Join(", ", r.ExistingPageTypes)}" : "Henüz sayfa tipi yok.")
            .AppendLine()
            .AppendLine("URL grupları (desen | sayfa sayısı | örnek adresler):");
        foreach (var g in r.Groups)
            text.AppendLine($"- {g.Pattern} | {g.Count} | {string.Join(" ", g.SampleUrls)}");
        return text.ToString();
    }

    public static IReadOnlyList<AiPageTypeSuggestion> Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<Output>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Model boş yanıt döndü.");
        return (dto.PageTypes ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p.Name) && !string.IsNullOrWhiteSpace(p.UrlGlob))
            .Take(MaxSuggestions)
            .Select(p => new AiPageTypeSuggestion(p.Name!.Trim(), p.UrlGlob!.Trim(), p.Reason?.Trim() ?? ""))
            .ToList();
    }

    private sealed record Output(List<OutputPageType>? PageTypes);

    private sealed record OutputPageType(string? Name, string? UrlGlob, string? Reason);
}
