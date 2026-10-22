using MediatR;

namespace MarketingOps.Application.Connectors.Commands.TriggerConnectorSync;

/// <summary>Bağlı connector'ların senkronunu hemen kuyruğa ekler. <see cref="IncludeWebVitals"/>: PageSpeed ölçümünü de çalıştır.</summary>
public sealed record TriggerConnectorSyncCommand(Guid SiteId, bool IncludeWebVitals = false) : IRequest;
