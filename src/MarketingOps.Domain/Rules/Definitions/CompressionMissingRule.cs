using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class CompressionMissingRule : RuleBase, IPageRule
{
    public override string Code => "perf.compression.missing";
    public override string Name => "Sıkıştırma yok";
    public override string Description => "HTML yanıtının br/gzip ile sıkıştırılıp sıkıştırılmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "perf-doc:p30";
    public override string Impact => "Sıkıştırma HTML aktarım boyutunu belirgin şekilde azaltır; yokluğu özellikle mobil bağlantıda sayfanın açılmasını geciktirir.";
    protected override string TitleTemplate => "{n} sayfada HTML sıkıştırılmadan gönderiliyor";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "HTML yanıtları sıkıştırılarak gönderilmeli.", "1. Sunucu ya da CDN'de HTML, CSS ve JS için Brotli (br) ya da gzip sıkıştırmasını açın.\n2. Yanıtta Content-Encoding başlığının döndüğünü doğrulayın.");
    public override string TeamCriterion => "Sunucu/CDN sıkıştırma ayarı açıldı.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || page.HtmlSizeBytes is not >= 1024) yield break;
        var encoding = page.ContentEncoding?.ToLowerInvariant();
        if (encoding is not ("br" or "gzip" or "deflate" or "zstd"))
            yield return Violation(page, encoding ?? "Content-Encoding yok", $"{(page.HtmlSizeBytes.Value / 1024).ToString("N0", Tr)} KB");
    }
}
