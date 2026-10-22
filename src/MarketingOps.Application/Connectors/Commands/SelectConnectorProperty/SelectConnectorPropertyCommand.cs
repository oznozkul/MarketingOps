using MediatR;

namespace MarketingOps.Application.Connectors.Commands.SelectConnectorProperty;

/// <summary>
/// Search Console ve GA4 mülkü seçimi ya da değişikliği. Her alan için: null → değişmez, boş → mülk kaldırılır, dolu → seçilir.
/// Seçilen Search Console mülkü bu sitenin domainine aitse ve kullanıcının doğrulanmış yetkisi varsa site sahipliği doğrulanır.
/// Mülk kaldırmak sahipliği geri almaz (sahiplik bir kez kanıtlandı).
/// </summary>
public sealed record SelectConnectorPropertyCommand(Guid SiteId, string? SearchConsoleSiteUrl, string? Ga4PropertyId) : IRequest;
