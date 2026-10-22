using MarketingOps.Domain.Findings;

namespace MarketingOps.Web.ViewModels.Design;

public sealed record DesignScoreDeduction(string Title, Severity Severity, string PageTypeName, int AffectedPages, int PageTypePages, decimal Deduction);
