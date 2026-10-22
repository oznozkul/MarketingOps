using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Connectors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Commands.CompleteGoogleConnect;

public sealed class CompleteGoogleConnectCommandHandler(IAppDbContext db, IGoogleAuthClient auth, ConnectorSecretProtector protector, TimeProvider clock)
    : IRequestHandler<CompleteGoogleConnectCommand, GoogleConnectResult>
{
    public async Task<GoogleConnectResult> Handle(CompleteGoogleConnectCommand request, CancellationToken cancellationToken)
    {
        var siteId = protector.ReadState(request.State);
        if (!await db.Sites.AnyAsync(s => s.Id == siteId, cancellationToken)) throw new NotFoundException("Site bulunamadı.");
        if (request.Error is not null || string.IsNullOrEmpty(request.Code))
            return new GoogleConnectResult(siteId, false, request.Error == "access_denied" ? "Google izni verilmedi." : $"Google bağlantısı tamamlanamadı ({request.Error}).");

        GoogleAuthorization authorization;
        try
        {
            authorization = await auth.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
        }
        catch (GoogleApiException ex)
        {
            return new GoogleConnectResult(siteId, false, ex.Message);
        }
        var secret = protector.Protect(authorization.RefreshToken);
        var now = clock.GetUtcNow();
        var accounts = await db.ConnectorAccounts.Where(a => a.SiteId == siteId && GoogleConnectors.Types.Contains(a.Type)).ToListAsync(cancellationToken);
        foreach (var type in GoogleConnectors.Types)
        {
            var account = accounts.FirstOrDefault(a => a.Type == type);
            if (account is null)
            {
                account = ConnectorAccount.Create(siteId, type, now);
                db.ConnectorAccounts.Add(account);
            }
            account.Authorize(secret, authorization.Email, authorization.Scopes, GoogleConnectors.RequiresProperty(type), now);
        }
        await db.SaveChangesAsync(cancellationToken);
        return new GoogleConnectResult(siteId, true, null);
    }
}
