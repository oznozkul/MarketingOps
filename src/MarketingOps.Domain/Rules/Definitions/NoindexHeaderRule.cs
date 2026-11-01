using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>Noindex'in yalnızca X-Robots-Tag başlığında verilip sayfa kaynağında (meta robots) olmadığı sayfalar.</summary>
public sealed class NoindexHeaderRule : RuleBase, IPageRule
{
    public override string Code => "seo.noindex.header";
    public override string Name => "Noindex yalnızca X-Robots-Tag başlığında";
    public override string Description => "Noindex'in yalnızca yanıt başlığında (X-Robots-Tag), HTML kaynağındaki meta robots'ta olmadığı sayfaları bulur; başlık sunucu ya da CDN ayarından gelebilir ve kaynakta görünmediği için gözden kaçar.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:robots";
    public override string Impact => "Kaynakta görünmeyen başlık noindex'i sayfayı sessizce dizinden çıkarır; şablonda aranınca bulunmadığı için hata ayıklamak zordur.";
    protected override string TitleTemplate => "{n} sayfada noindex yalnızca başlıkta";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Noindex kaynağında meta robots'ta verilmeli ya da başlıktaki kural bilinçli olarak kaldırılmalı.", "1. Sayfanın indekslenmesi isteniyorsa X-Robots-Tag başlığını kaldırın.\n2. İstenmiyorsa noindex'i meta robots ile de kaynağa ekleyin ki izlenebilir olsun.\n3. Sunucu/CDN'deki genel noindex kurallarını gözden geçirin.");
    public override string TeamCriterion => "Noindex sinyali kaynaktaki meta robots ile tutarlı.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || !PageChecks.HasSignals(page)) yield break;
        var header = page.Data.RobotsHeader;
        if (!PageChecks.RobotsHeaderHasNoindex(header)) yield break;
        if (page.Data.MetaRobots?.Contains("noindex", StringComparison.OrdinalIgnoreCase) == true) yield break;
        yield return Violation(page, header, "noindex yalnızca X-Robots-Tag başlığında");
    }
}
