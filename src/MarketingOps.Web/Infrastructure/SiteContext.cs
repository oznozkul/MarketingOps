using MarketingOps.Application.Sites.Queries.GetSites;

namespace MarketingOps.Web.Infrastructure;

public class SiteContext : ISiteContext
{
    public SiteListItemResponse? Site { get; set; }

    public IReadOnlyList<SiteListItemResponse> Sites { get; set; } = [];
}
