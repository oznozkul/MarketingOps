using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Connectors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Connectors.Queries.GetGoogleProperties;

public sealed class GetGooglePropertiesQueryHandler(IAppDbContext db, GoogleTokenProvider tokens, ISearchConsoleClient searchConsole, IGa4Client ga4)
    : IRequestHandler<GetGooglePropertiesQuery, GooglePropertiesResponse>
{
    public async Task<GooglePropertiesResponse> Handle(GetGooglePropertiesQuery request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.Where(s => s.Id == request.SiteId).Select(s => new { s.Domain, s.Name }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Site bulunamadı.");
        var domain = site.Domain;
        // Token yenileme bağlantıyı "süresi doldu" işaretleyebileceği için takip edilen kayıtlar kullanılır.
        var accounts = await db.ConnectorAccounts.Where(a => a.SiteId == request.SiteId && a.ProtectedSecret != null).ToListAsync(cancellationToken);
        var sc = accounts.FirstOrDefault(a => a.Type == ConnectorType.SearchConsole);
        var analytics = accounts.FirstOrDefault(a => a.Type == ConnectorType.Ga4);
        if (sc is null && analytics is null) throw new ConflictException("Önce Google ile bağlanın.");

        try
        {
            var sites = sc is null ? [] : await searchConsole.ListSitesAsync(await tokens.AccessTokenAsync(sc, cancellationToken), cancellationToken);
            var properties = analytics is null ? [] : await ga4.ListPropertiesAsync(await tokens.AccessTokenAsync(analytics, cancellationToken), cancellationToken);
            return new GooglePropertiesResponse(
                (sc ?? analytics)!.AccountEmail,
                sites.Select(s => new SearchConsolePropertyResponse(s.SiteUrl, s.PermissionLevel, s.IsVerified, GoogleConnectors.MatchesSite(s.SiteUrl, domain)))
                    .OrderByDescending(s => s.MatchesSite).ThenBy(s => s.SiteUrl).ToList(),
                properties.Select(p => new Ga4PropertyResponse(p.Id, p.DisplayName, p.AccountName, LikelyMatch(p, site.Name, domain), p.AccountName == DemoAccount))
                    .OrderByDescending(p => p.LikelyMatch).ThenBy(p => p.IsDemo).ThenBy(p => p.AccountName).ThenBy(p => p.DisplayName).ToList(),
                sc?.ExternalAccountId,
                analytics?.ExternalAccountId);
        }
        finally
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private const string DemoAccount = "Demo Account";

    /// <summary>"Lastik Borsası" sitesi ↔ "Lastik Borsası - GA4" mülkü; "lastikborsasi" domain kökü ↔ "lastikborsasi-web".</summary>
    private static bool LikelyMatch(Ga4Property property, string siteName, string domain)
    {
        var root = domain.Split('.')[0];
        var text = $"{property.DisplayName} {property.AccountName}";
        return text.Contains(siteName, StringComparison.CurrentCultureIgnoreCase) || Compact(text).Contains(Compact(root), StringComparison.OrdinalIgnoreCase);
    }

    private static string Compact(string value) =>
        new(value.ToLowerInvariant().Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c').Where(char.IsLetterOrDigit).ToArray());
}
