using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class RenderBlockingScriptRule : RuleBase, IPageRule
{
    public override string Code => "perf.render_blocking_script";
    public override string Name => "Render'ı bloklayan script";
    public override string Description => "<head> içinde async/defer olmadan yüklenen harici script'leri bulur.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "perf-doc:p22";
    public override string Impact => "Tarayıcı bu dosyalar inip çalışana kadar sayfayı çizemez; ilk görüntü gecikir.";
    protected override string TitleTemplate => "{n} sayfada render'ı bloklayan script";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Head'deki harici scriptler sayfanın çizimini bekletmemeli; defer ya da async ile yüklenmeli.", "<script src=\"/js/uygulama.js\" defer></script>");
    public override string TeamCriterion => "Head'deki scriptler defer/async ile yükleniyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page)) yield break;
        var blocking = page.Data.Scripts.Where(s => s.InHead && s.Src is not null && !s.Async && !s.Defer).ToList();
        if (blocking.Count > 0) yield return Violation(page, $"{blocking.Count} script", string.Join(", ", blocking.Take(5).Select(s => s.Src)));
    }
}
