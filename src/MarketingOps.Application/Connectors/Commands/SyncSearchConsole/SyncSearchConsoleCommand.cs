using MediatR;

namespace MarketingOps.Application.Connectors.Commands.SyncSearchConsole;

/// <summary>
/// Günlük senkron: son 28 günün sayfa performansı (upsert), sitemap durumu ve önemli sayfalar için URL Inspection
/// (günlük kotaya göre; öncelik açık bulgusu olan sayfalarda, sonra en çok gösterim alanlarda).
/// </summary>
public sealed record SyncSearchConsoleCommand(Guid SiteId) : IRequest;
