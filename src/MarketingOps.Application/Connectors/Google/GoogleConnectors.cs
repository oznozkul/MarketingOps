using MarketingOps.Domain.Connectors;

namespace MarketingOps.Application.Connectors.Google;

/// <summary>Tek Google yetkisiyle bağlanan connector'lar ve istenen salt okunur scope'lar.</summary>
public static class GoogleConnectors
{
    public static IReadOnlyList<ConnectorType> Types { get; } = [ConnectorType.SearchConsole, ConnectorType.Ga4, ConnectorType.Gtm];

    public static IReadOnlyList<string> Scopes { get; } =
    [
        "openid",
        "email",
        "https://www.googleapis.com/auth/webmasters.readonly",
        "https://www.googleapis.com/auth/analytics.readonly",
        "https://www.googleapis.com/auth/tagmanager.readonly"
    ];

    /// <summary>GA4 e-ticaret sağlığı için izlenen event'ler.</summary>
    public static IReadOnlyList<string> Ga4Events { get; } =
        ["view_item", "add_to_cart", "begin_checkout", "purchase", "login", "sign_up", "search", "generate_lead"];

    /// <summary>Mülk seçimi gerektiren connector'lar; GTM'de container seçimi tracking denetiminde yapılır.</summary>
    public static bool RequiresProperty(ConnectorType type) => type is ConnectorType.SearchConsole or ConnectorType.Ga4;

    /// <summary>"sc-domain:ornek.com" ya da "https://www.ornek.com/" mülkü bu siteye mi ait.</summary>
    public static bool MatchesSite(string siteUrl, string domain)
    {
        var host = siteUrl.StartsWith("sc-domain:", StringComparison.OrdinalIgnoreCase)
            ? siteUrl["sc-domain:".Length..]
            : Uri.TryCreate(siteUrl, UriKind.Absolute, out var url) ? url.Host : siteUrl;
        host = host.Trim().TrimEnd('/').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];
        return host == domain;
    }
}
