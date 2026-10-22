namespace MarketingOps.Application.Connectors.Queries.GetConnectors;

/// <summary><see cref="GoogleConfigured"/>: uygulamanın Google OAuth istemcisi ayarlı mı (değilse bağlama başlatılamaz).</summary>
public sealed record ConnectorsResponse(bool GoogleConfigured, IReadOnlyList<ConnectorResponse> Connectors, SitemapSummaryResponse? Sitemaps);
