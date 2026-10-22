using MediatR;

namespace MarketingOps.Application.Findings.Queries.GetFindingSummary;

/// <summary>Liste üstündeki özet şerit: bu filtrede kaç bulgu var, kaçı kritik, kaçı son taramada yeni.</summary>
public sealed record GetFindingSummaryQuery(Guid SiteId, FindingFilter Filter) : IRequest<FindingSummaryResponse>;
