using MediatR;

namespace MarketingOps.Application.Connectors.Commands.SyncGa4;

/// <summary>Günlük senkron: son 30 günün e-ticaret event sayıları ve değerleri (upsert).</summary>
public sealed record SyncGa4Command(Guid SiteId) : IRequest;
