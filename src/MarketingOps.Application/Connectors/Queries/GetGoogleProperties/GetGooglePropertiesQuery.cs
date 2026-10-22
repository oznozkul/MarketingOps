using MediatR;

namespace MarketingOps.Application.Connectors.Queries.GetGoogleProperties;

/// <summary>Bağlı Google hesabının erişebildiği Search Console ve GA4 mülkleri (canlı olarak Google'dan).</summary>
public sealed record GetGooglePropertiesQuery(Guid SiteId) : IRequest<GooglePropertiesResponse>;
