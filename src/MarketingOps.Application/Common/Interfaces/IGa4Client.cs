using MarketingOps.Application.Connectors.Google;

namespace MarketingOps.Application.Common.Interfaces;

public interface IGa4Client
{
    Task<IReadOnlyList<Ga4Property>> ListPropertiesAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>Verilen event'lerin gün bazında sayısı ve değeri.</summary>
    Task<IReadOnlyList<Ga4EventRow>> RunEventReportAsync(string accessToken, string propertyId, DateOnly start, DateOnly end,
        IReadOnlyList<string> eventNames, CancellationToken cancellationToken);
}
