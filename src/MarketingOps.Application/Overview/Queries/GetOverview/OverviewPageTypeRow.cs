namespace MarketingOps.Application.Overview.Queries.GetOverview;

public sealed record OverviewPageTypeRow(Guid Id, string Name, int PageCount, int OpenFindings, int SeoScore);
