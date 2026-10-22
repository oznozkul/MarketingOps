using MediatR;

namespace MarketingOps.Application.Tasks.Commands.RequestTaskEnrichment;

/// <summary>"Öneriyi AI ile yeniden üret": zenginleştirme işini kuyruğa ekler.</summary>
public sealed record RequestTaskEnrichmentCommand(Guid SiteId, Guid TaskId) : IRequest;
