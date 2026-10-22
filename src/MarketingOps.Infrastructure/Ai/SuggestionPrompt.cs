using System.Text;
using System.Text.Json;
using MarketingOps.Application.Tasks.Suggestions;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Infrastructure.Ai;

/// <summary>
/// İki sağlayıcının ortak istemi ve çıktı şeması. Model yalnızca verilen veriyi kullanır; sayılar ve örnek URL'ler
/// şablondan gelir, model yeni sayı uydurmaz. Çıktı her zaman doğrulanır.
/// </summary>
public static class SuggestionPrompt
{
    public const string System =
        """
        Bir e-ticaret sitesinin SEO, performans ve tracking denetim aracında, ekibe açılan görevlerin "Etki" ve "Önerilen çözüm" metinlerini yazıyorsun.
        Kurallar:
        - Türkçe yaz, sade ve uygulanabilir ol. Geliştirici başka bir yere bakmadan işi yapabilmeli.
        - Yalnızca sana verilen veriyi kullan. Verilmeyen hiçbir sayı, yüzde, trafik, sıralama, sipariş ya da gelir değeri yazma; tahmin etme.
        - Sınır ve eşik olarak yalnızca "Kuralın bu sitedeki eşikleri" satırındaki değerleri kullan.
        - Sayfa sayıları ve örnek URL'ler verildiği gibidir; onları değiştirme.
        - Araç siteye hiçbir şey yazmaz; öneri ekibin uygulaması içindir. "Uygula", "Düzelt" gibi aracın işi yapacağını ima eden ifadeler kullanma.
        - Bilgi tabanı parçası verildiyse önerini ona dayandır.
        - Şablon öneriyi aynen kopyalama; bu sayfa grubuna ve örneklere özgü, somut bir öneri yaz.
        - Önerilen değerleri sayfanın mevcut değerine ve ayrıntısına (ör. asıl title metni) dayandır. URL'deki kısaltmaları yorumlamaya çalışma; anlamından emin olmadığın kelimeyi değiştirme.
        Yanıtın yalnızca istenen JSON nesnesi olsun.
        """;

    /// <summary>JSON şeması (Anthropic yapılandırılmış çıktı) ve OpenAI uyumlu sağlayıcılarda istemde tarif edilen biçim.</summary>
    public static readonly JsonElement Schema = JsonSerializer.SerializeToElement(new Dictionary<string, object>
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = new[] { "impact", "summary", "content", "suggestedValues" },
        ["properties"] = new Dictionary<string, object>
        {
            ["impact"] = new { type = "string", description = "Sorunun neden önemli olduğu; 2-4 cümle, sayı uydurmadan." },
            ["summary"] = new { type = "string", description = "Çözümün tek cümlelik özeti." },
            ["content"] = new { type = "string", description = "Öneri içeriği: metin kalıbı, kod bloğu ya da numaralı adımlar (istenen türe göre)." },
            ["suggestedValues"] = new
            {
                type = "array",
                description = "Yalnızca metin önerilerinde: verilen örnek URL'ler için önerilen yeni değer.",
                items = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["required"] = new[] { "url", "suggested" },
                    ["properties"] = new Dictionary<string, object> { ["url"] = new { type = "string" }, ["suggested"] = new { type = "string" } }
                }
            }
        }
    });

    public static string User(AiSuggestionRequest r, IReadOnlyList<string> knowledge)
    {
        var text = new StringBuilder()
            .AppendLine($"Kural: {r.RuleName} ({r.RuleCode})")
            .AppendLine($"Ne kontrol eder: {r.RuleDescription}")
            .AppendLine($"Sayfa grubu: {r.PageTypeName ?? "Hiçbir sayfa tipine uymayan sayfalar"}")
            .AppendLine($"Etkilenen sayfa sayısı: {r.AffectedCount}")
            .AppendLine($"Problem: {r.Problem}")
            .AppendLine($"Genel etki açıklaması (şablon): {r.TemplateImpact}")
            .AppendLine(r.Thresholds.Count > 0 ? $"Kuralın bu sitedeki eşikleri: {string.Join("; ", r.Thresholds)}" : "Kuralın ayarlanabilir eşiği yok.")
            .AppendLine()
            .AppendLine($"İstenen öneri türü: {Kind(r.Kind)}")
            .AppendLine($"Şablon öneri özeti: {r.TemplateSummary}")
            .AppendLine("Şablon öneri içeriği:").AppendLine(r.TemplateContent)
            .AppendLine();
        if (r.Examples.Count > 0)
        {
            text.AppendLine("Örnek sayfalar (URL | mevcut değer | ayrıntı):");
            foreach (var e in r.Examples) text.AppendLine($"- {e.Url} | {e.Current ?? "-"} | {e.Detail ?? "-"}");
            text.AppendLine();
        }
        if (knowledge.Count > 0)
        {
            text.AppendLine($"Bilgi tabanı ({r.ReferenceKey}):");
            foreach (var chunk in knowledge) text.AppendLine(chunk).AppendLine("---");
        }
        text.AppendLine(r.Kind == SuggestionKind.Text
            ? "suggestedValues alanında yukarıdaki örnek URL'lerin her biri için, mevcut metni temel alan ve sorunu gideren (eşiklere uyan) yeni değeri ver; mevcut değeri aynen tekrar etme."
            : "suggestedValues alanını boş dizi bırak.");
        return text.ToString();
    }

    /// <summary>Model çıktısını doğrular: zorunlu alanlar dolu olmalı, önerilen değerler yalnızca verilen URL'ler için olmalı.</summary>
    public static AiSuggestion Parse(string json, AiSuggestionRequest request)
    {
        var dto = JsonSerializer.Deserialize<Output>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Model boş yanıt döndü.");
        if (string.IsNullOrWhiteSpace(dto.Content)) throw new InvalidOperationException("Model öneri içeriği üretmedi.");
        var urls = request.Examples.Select(e => e.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var values = (dto.SuggestedValues ?? [])
            .Where(v => !string.IsNullOrWhiteSpace(v.Url) && !string.IsNullOrWhiteSpace(v.Suggested) && urls.Contains(v.Url!))
            .Select(v => new AiSuggestedValue(v.Url!, v.Suggested!.Trim()))
            .ToList();
        return new AiSuggestion(dto.Impact ?? "", dto.Summary ?? "", dto.Content!, values);
    }

    private static string Kind(SuggestionKind kind) => kind switch
    {
        SuggestionKind.Code => "kod (content alanına kopyalanabilir kod bloğu yaz, açıklamayı summary'ye koy)",
        SuggestionKind.Steps => "adım listesi (content alanına numaralı adımlar yaz, her adım ayrı satırda)",
        _ => "metin kalıbı (content alanına önerilen kalıbı ve kısa uygulama notunu yaz)"
    };

    private sealed record Output(string? Impact, string? Summary, string? Content, List<OutputValue>? SuggestedValues);

    private sealed record OutputValue(string? Url, string? Suggested);
}
