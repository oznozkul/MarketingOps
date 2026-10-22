using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Sites;
using MarketingOps.Domain.Connectors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Commands.ConnectMeta;

public sealed class ConnectMetaCommandHandler(IAppDbContext db, ConnectorSecretProtector protector, TimeProvider clock) : IRequestHandler<ConnectMetaCommand>
{
    public async Task Handle(ConnectMetaCommand request, CancellationToken cancellationToken)
    {
        var site = await SiteLoader.LoadAsync(db, request.SiteId, cancellationToken);
        var now = clock.GetUtcNow();
        var account = await db.ConnectorAccounts.FirstOrDefaultAsync(a => a.SiteId == request.SiteId && a.Type == ConnectorType.Meta, cancellationToken);
        if (account is null)
        {
            account = ConnectorAccount.Create(request.SiteId, ConnectorType.Meta, now);
            db.ConnectorAccounts.Add(account);
        }

        if (request.Remove) account.Disconnect(now);
        else if (!string.IsNullOrWhiteSpace(request.AccessToken))
            account.ConnectToken(protector.Protect(request.AccessToken.Trim()), site.ExpectedTrackingIds.MetaPixelId, now);
        await db.SaveChangesAsync(cancellationToken);
    }
}
