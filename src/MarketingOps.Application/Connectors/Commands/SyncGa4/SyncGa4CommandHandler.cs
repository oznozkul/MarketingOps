using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.Connectors.Commands.SyncGa4;

public sealed class SyncGa4CommandHandler(IAppDbContext db, GoogleTokenProvider tokens, IGa4Client client, TimeProvider clock,
    ILogger<SyncGa4CommandHandler> logger) : IRequestHandler<SyncGa4Command>
{
    private const int WindowDays = 30;

    public async Task Handle(SyncGa4Command request, CancellationToken cancellationToken)
    {
        var account = await db.ConnectorAccounts.FirstOrDefaultAsync(a => a.SiteId == request.SiteId && a.Type == ConnectorType.Ga4
            && a.ExternalAccountId != null && a.ProtectedSecret != null, cancellationToken);
        if (account is null) return;

        try
        {
            var token = await tokens.AccessTokenAsync(account, cancellationToken);
            var end = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-1);
            var start = end.AddDays(-(WindowDays - 1));
            var rows = await client.RunEventReportAsync(token, account.ExternalAccountId!, start, end, GoogleConnectors.Ga4Events, cancellationToken);
            var existing = (await db.Ga4Events.Where(e => e.SiteId == request.SiteId && e.Date >= start && e.Date <= end).ToListAsync(cancellationToken))
                .ToDictionary(e => (e.Date, e.EventName));
            foreach (var row in rows)
            {
                if (existing.TryGetValue((row.Date, row.EventName), out var current)) current.Update(row.Count, row.Value);
                else db.Ga4Events.Add(Ga4EventDaily.Create(request.SiteId, row.Date, row.EventName, row.Count, row.Value));
            }
            account.RecordSync(clock.GetUtcNow());
        }
        catch (GoogleApiException ex)
        {
            logger.LogWarning("GA4 senkronu başarısız ({SiteId}): {Message}", request.SiteId, ex.Message);
            if (!ex.IsAuthorizationError) account.RecordFailure(ex.Message, authorizationExpired: false, clock.GetUtcNow());
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
