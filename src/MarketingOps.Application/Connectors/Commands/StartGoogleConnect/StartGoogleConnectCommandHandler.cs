using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Commands.StartGoogleConnect;

public sealed class StartGoogleConnectCommandHandler(IAppDbContext db, IGoogleAuthClient auth, ConnectorSecretProtector protector)
    : IRequestHandler<StartGoogleConnectCommand, string>
{
    public async Task<string> Handle(StartGoogleConnectCommand request, CancellationToken cancellationToken)
    {
        if (!auth.IsConfigured) throw new ConflictException("Google bağlantısı bu kurulumda ayarlı değil (OAuth istemcisi eksik).");
        if (!await db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken)) throw new NotFoundException("Site bulunamadı.");
        return auth.BuildAuthorizationUrl(protector.CreateState(request.SiteId), request.RedirectUri);
    }
}
