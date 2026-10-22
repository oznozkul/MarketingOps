using MediatR;

namespace MarketingOps.Application.Connectors.Commands.ConnectMeta;

/// <summary>Token boşsa Meta bağlantısı kaldırılır. Pixel kimliği sitedeki beklenen kimlikten okunur.</summary>
/// <summary>Token boş ve kaldırma istenmemişse mevcut bağlantı durur.</summary>
public sealed record ConnectMetaCommand(Guid SiteId, string? AccessToken, bool Remove) : IRequest;
