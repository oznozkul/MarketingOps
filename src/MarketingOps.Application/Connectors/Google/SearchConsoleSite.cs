namespace MarketingOps.Application.Connectors.Google;

/// <summary>Search Console mülkü. <see cref="PermissionLevel"/>: siteOwner, siteFullUser, siteRestrictedUser, siteUnverifiedUser.</summary>
public sealed record SearchConsoleSite(string SiteUrl, string PermissionLevel)
{
    public bool IsVerified => PermissionLevel != "siteUnverifiedUser";
}
