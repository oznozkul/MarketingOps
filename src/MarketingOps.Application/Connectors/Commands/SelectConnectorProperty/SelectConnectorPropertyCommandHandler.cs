using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Connectors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Commands.SelectConnectorProperty;

public sealed class SelectConnectorPropertyCommandHandler(IAppDbContext db, GoogleTokenProvider tokens, ISearchConsoleClient searchConsole,
    IGa4Client ga4, ICrawlScheduler scheduler, TimeProvider clock) : IRequestHandler<SelectConnectorPropertyCommand>
{
    public async Task Handle(SelectConnectorPropertyCommand request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == request.SiteId, cancellationToken) ?? throw new NotFoundException("Site bulunamadı.");
        var accounts = await db.ConnectorAccounts.Where(a => a.SiteId == site.Id).ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();

        // Seçim kullanıcının gönderdiği değere değil, Google'ın o hesap için döndürdüğü listeye göre doğrulanır.
        if (request.SearchConsoleSiteUrl == "") Clear(accounts, ConnectorType.SearchConsole, now);
        else if (request.SearchConsoleSiteUrl is not null)
        {
            var account = Authorized(accounts, ConnectorType.SearchConsole);
            var sites = await searchConsole.ListSitesAsync(await tokens.AccessTokenAsync(account, cancellationToken), cancellationToken);
            var selected = sites.FirstOrDefault(s => s.SiteUrl == request.SearchConsoleSiteUrl)
                ?? throw new DomainException("Seçilen Search Console mülkü bu Google hesabında bulunamadı.");
            account.SelectProperty(selected.SiteUrl, selected.SiteUrl, now);
            if (selected.IsVerified && GoogleConnectors.MatchesSite(selected.SiteUrl, site.Domain)) site.MarkOwnershipVerified();
        }

        if (request.Ga4PropertyId == "") Clear(accounts, ConnectorType.Ga4, now);
        else if (request.Ga4PropertyId is not null)
        {
            var account = Authorized(accounts, ConnectorType.Ga4);
            var properties = await ga4.ListPropertiesAsync(await tokens.AccessTokenAsync(account, cancellationToken), cancellationToken);
            var selected = properties.FirstOrDefault(p => p.Id == request.Ga4PropertyId)
                ?? throw new DomainException("Seçilen GA4 mülkü bu Google hesabında bulunamadı.");
            account.SelectProperty(selected.Id, $"{selected.AccountName} › {selected.DisplayName}", now);
        }

        await db.SaveChangesAsync(cancellationToken);
        scheduler.EnqueueConnectorSync(site.Id);
    }

    private static void Clear(IEnumerable<ConnectorAccount> accounts, ConnectorType type, DateTimeOffset now) =>
        accounts.FirstOrDefault(a => a.Type == type)?.ClearProperty(now);

    private static ConnectorAccount Authorized(IEnumerable<ConnectorAccount> accounts, ConnectorType type) =>
        accounts.FirstOrDefault(a => a.Type == type && a.HasSecret) ?? throw new ConflictException("Önce Google ile bağlanın.");
}
