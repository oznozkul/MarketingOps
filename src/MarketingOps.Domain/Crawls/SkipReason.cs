namespace MarketingOps.Domain.Crawls;

public enum SkipReason
{
    RobotsTxt,
    ExcludedPath,
    MaxPagesReached,
    ExternalHost
}
