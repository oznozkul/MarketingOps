using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Connectors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Queries.GetConnectors;

public sealed class GetConnectorsQueryHandler(IAppDbContext db, IGoogleAuthClient auth) : IRequestHandler<GetConnectorsQuery, ConnectorsResponse>
{
    public async Task<ConnectorsResponse> Handle(GetConnectorsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await db.ConnectorAccounts.AsNoTracking().Where(a => a.SiteId == request.SiteId)
            .ToDictionaryAsync(a => a.Type, cancellationToken);
        var connectors = Enum.GetValues<ConnectorType>().Select(type => accounts.TryGetValue(type, out var a)
                ? new ConnectorResponse(type, a.Status, a.AccountEmail, a.ExternalAccountId, a.ExternalAccountName, a.Scopes, a.LastSyncAt, a.LastError)
                : new ConnectorResponse(type, ConnectorStatus.NotConnected, null, null, null, [], null, null))
            .ToList();

        var sitemaps = await db.Sitemaps.AsNoTracking().Where(s => s.SiteId == request.SiteId).ToListAsync(cancellationToken);
        var summary = sitemaps.Count == 0 ? null : new SitemapSummaryResponse(sitemaps.Count, sitemaps.Sum(s => s.Errors), sitemaps.Sum(s => s.Warnings),
            sitemaps.Sum(s => s.Submitted), sitemaps.Sum(s => s.Indexed), sitemaps.Max(s => s.SyncedAt));
        return new ConnectorsResponse(auth.IsConfigured, connectors, summary);
    }
}
