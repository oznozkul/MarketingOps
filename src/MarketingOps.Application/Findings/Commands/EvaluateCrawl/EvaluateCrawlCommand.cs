using MediatR;

namespace MarketingOps.Application.Findings.Commands.EvaluateCrawl;

/// <summary>Taramadan sonra işçi tarafından çalıştırılır: kuralları uygular ve bulguları senkronize eder.</summary>
public sealed record EvaluateCrawlCommand(Guid CrawlId) : IRequest;
