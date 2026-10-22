using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateOrderSource;

public sealed class UpdateOrderSourceCommandHandler(IAppDbContext db, ConnectorSecretProtector protector) : IRequestHandler<UpdateOrderSourceCommand>
{
    public async Task Handle(UpdateOrderSourceCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            site.UpdateOrderSource(OrderSourceSettings.None);
        }
        else
        {
            var connection = string.IsNullOrWhiteSpace(request.ConnectionString)
                ? site.OrderSource.ProtectedConnection
                : protector.Protect(request.ConnectionString.Trim());
            if (connection is null) throw new DomainException("Sipariş kaynağı için bağlantı cümlesi gerekli.");
            site.UpdateOrderSource(new OrderSourceSettings(connection, request.Query));
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
