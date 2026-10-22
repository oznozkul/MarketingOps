namespace MarketingOps.Domain.Rules;

public enum RuleScope
{
    /// <summary>Her sayfa tek başına değerlendirilir.</summary>
    Page,

    /// <summary>Taramadaki sayfalara birlikte bakılır (ör. tekrar eden title, ortalama TTFB).</summary>
    Site
}
