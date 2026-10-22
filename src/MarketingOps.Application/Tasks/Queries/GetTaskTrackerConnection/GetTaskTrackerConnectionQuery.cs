using MediatR;

namespace MarketingOps.Application.Tasks.Queries.GetTaskTrackerConnection;

/// <summary>Bağlantı yoksa null. Anahtarın kendisi hiçbir zaman dönmez.</summary>
public sealed record GetTaskTrackerConnectionQuery(Guid SiteId) : IRequest<TaskTrackerConnectionResponse?>;
