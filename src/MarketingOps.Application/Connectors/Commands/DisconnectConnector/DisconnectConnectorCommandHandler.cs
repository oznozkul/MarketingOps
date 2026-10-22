using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Commands.DisconnectConnector;

public sealed class DisconnectConnectorCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<DisconnectConnectorCommand>
{
    public async Task Handle(DisconnectConnectorCommand request, CancellationToken cancellationToken)
    {
        var account = await db.ConnectorAccounts.FirstOrDefaultAsync(a => a.SiteId == request.SiteId && a.Type == request.Type, cancellationToken);
        if (account is null) return;
        account.Disconnect(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
