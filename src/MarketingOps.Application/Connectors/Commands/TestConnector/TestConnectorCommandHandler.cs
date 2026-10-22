using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Connectors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Commands.TestConnector;

public sealed class TestConnectorCommandHandler(IAppDbContext db, GoogleTokenProvider tokens, ISearchConsoleClient searchConsole, IGa4Client ga4,
    TimeProvider clock) : IRequestHandler<TestConnectorCommand, ConnectorTestResult>
{
    public async Task<ConnectorTestResult> Handle(TestConnectorCommand request, CancellationToken cancellationToken)
    {
        var account = await db.ConnectorAccounts.FirstOrDefaultAsync(a => a.SiteId == request.SiteId && a.Type == request.Type && a.ProtectedSecret != null, cancellationToken)
            ?? throw new ConflictException("Bağlantı kurulmamış.");
        ConnectorTestResult result;
        try
        {
            var token = await tokens.AccessTokenAsync(account, cancellationToken);
            result = account.Type switch
            {
                ConnectorType.SearchConsole => Check((await searchConsole.ListSitesAsync(token, cancellationToken)).Select(s => s.SiteUrl), account),
                ConnectorType.Ga4 => Check((await ga4.ListPropertiesAsync(token, cancellationToken)).Select(p => p.Id), account),
                _ => new ConnectorTestResult(true, "Google yetkisi geçerli.")
            };
            if (result.Ok) account.RecordSuccessfulTest(clock.GetUtcNow());
            else account.RecordFailure(result.Message, authorizationExpired: false, clock.GetUtcNow());
        }
        catch (GoogleApiException ex)
        {
            if (!ex.IsAuthorizationError) account.RecordFailure(ex.Message, authorizationExpired: false, clock.GetUtcNow());
            result = new ConnectorTestResult(false, ex.Message);
        }
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static ConnectorTestResult Check(IEnumerable<string> available, ConnectorAccount account) =>
        account.ExternalAccountId is null ? new ConnectorTestResult(true, "Yetki geçerli; mülk henüz seçilmedi.")
        : available.Contains(account.ExternalAccountId) ? new ConnectorTestResult(true, "Bağlantı çalışıyor; seçili mülke erişilebiliyor.")
        : new ConnectorTestResult(false, "Seçili mülke artık bu Google hesabıyla erişilemiyor.");
}
