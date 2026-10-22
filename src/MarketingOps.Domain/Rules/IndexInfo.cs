namespace MarketingOps.Domain.Rules;

public sealed record IndexInfo(string Verdict, string? CoverageState)
{
    public bool IsIndexed => Verdict == Metrics.UrlIndexStatus.PassVerdict;
}
