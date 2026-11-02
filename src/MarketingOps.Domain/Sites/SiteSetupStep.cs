namespace MarketingOps.Domain.Sites;

/// <summary>Yeni site sihirbazının adımları, sırasıyla. Domain adımı siteyi oluşturduğu için burada yoktur.</summary>
public enum SiteSetupStep
{
    SearchConsole,
    Connectors,
    PageTypes,
    CrawlSettings,
    FirstCrawl
}
