using System.Globalization;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules;

/// <summary>Kural metadatasının ortak kısmı; her kural yalnızca kendine özgü değerlendirmeyi yazar.</summary>
public abstract class RuleBase : IRule
{
    protected static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public abstract string Code { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract FindingCategory Category { get; }
    public abstract Severity DefaultSeverity { get; }
    public abstract RuleScope Scope { get; }
    public virtual IReadOnlyList<RuleThreshold> Thresholds => [];
    public virtual RuleDataSource DataSource => RuleDataSource.Crawl;
    public abstract string ReferenceKey { get; }
    public abstract string Impact { get; }
    public abstract Team Team { get; }
    public abstract SuggestionTemplate Suggestion { get; }
    public abstract string TeamCriterion { get; }
    protected abstract string TitleTemplate { get; }

    public string Title(int affectedCount) => TitleTemplate.Replace("{n}", affectedCount.ToString("N0", Tr));

    protected RuleViolation Violation(PageSnapshot page, string? current, string? detail = null) =>
        new(Code, page.PageTypeId, page.Url, current, detail);

    /// <summary>İşaretleme sorununun sayısı ve ekibin öğeyi bulacağı örnekler.</summary>
    protected RuleViolation Violation(PageSnapshot page, ElementIssue issue) =>
        Violation(page, $"{issue.Count.ToString("N0", Tr)} öğe", string.Join(" · ", issue.Samples));
}
