using MarketingOps.Domain.Findings;

namespace MarketingOps.Web.ViewModels.Design;

public sealed record DesignFindingRow(string Title, string RuleCode, string PageTypeName, Severity Severity, FindingStatus Status, int AffectedPageCount, DateTimeOffset LastSeenAt);
