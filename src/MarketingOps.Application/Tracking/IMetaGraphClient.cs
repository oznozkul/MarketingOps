using MarketingOps.Domain.Rules.Definitions;

namespace MarketingOps.Application.Tracking;

/// <summary>Meta Graph'tan salt okunur pixel kanıtı. Kanıtlanamayan alanlar boş döner.</summary>
public interface IMetaGraphClient
{
    Task<MetaSignals> ReadAsync(string accessToken, string pixelId, CancellationToken cancellationToken);
}
