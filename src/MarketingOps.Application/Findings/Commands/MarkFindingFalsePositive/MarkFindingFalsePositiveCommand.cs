using MediatR;

namespace MarketingOps.Application.Findings.Commands.MarkFindingFalsePositive;

/// <summary>Kural bu sayfa tipi için susturulur: sonraki taramalar bulgunun durumunu değiştirmez.</summary>
public sealed record MarkFindingFalsePositiveCommand(Guid SiteId, Guid FindingId, string Reason, string Actor) : IRequest;
