using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class OpenGraphIncompleteRule : RuleBase, IPageRule
{
    public override string Code => "social.og.incomplete";
    public override string Name => "Paylaşım etiketleri eksik";
    public override string Description => "og:title, og:description, og:image, og:url ve og:type etiketlerinin dolu olduğunu, og:image ve og:url'in mutlak http(s) adres olduğunu kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "social:open-graph";
    public override string Impact => "Eksik paylaşım etiketleriyle sayfa WhatsApp, Facebook, LinkedIn ve X'te başlıksız ya da görselsiz bir bağlantı olarak görünür; tıklanma düşer.";
    protected override string TitleTemplate => "{n} sayfada paylaşım etiketleri eksik";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Her şablon beş temel Open Graph etiketini sayfanın kendi değerleriyle üretmeli.", "<meta property=\"og:type\" content=\"product\" />\n<meta property=\"og:title\" content=\"{Sayfa başlığı}\" />\n<meta property=\"og:description\" content=\"{Sayfa açıklaması}\" />\n<meta property=\"og:url\" content=\"{Canonical adres}\" />\n<meta property=\"og:image\" content=\"https://alanadi.com/{görsel}.jpg\" />");
    public override string TeamCriterion => "Şablonlar beş Open Graph etiketini mutlak adreslerle üretiyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var og = page.Data.OpenGraph;
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(og?.Title)) problems.Add("og:title yok");
        if (string.IsNullOrWhiteSpace(og?.Description)) problems.Add("og:description yok");
        if (string.IsNullOrWhiteSpace(og?.Image)) problems.Add("og:image yok");
        else if (!IsAbsoluteHttp(og.Image)) problems.Add("og:image mutlak adres değil");
        if (string.IsNullOrWhiteSpace(og?.Url)) problems.Add("og:url yok");
        else if (!IsAbsoluteHttp(og.Url)) problems.Add("og:url mutlak adres değil");
        if (string.IsNullOrWhiteSpace(og?.Type)) problems.Add("og:type yok");
        if (problems.Count > 0) yield return Violation(page, $"{problems.Count} sorun", string.Join(", ", problems));
    }

    private static bool IsAbsoluteHttp(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
