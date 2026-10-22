using MarketingOps.Application.Sites.Queries.GetSites;

namespace MarketingOps.Web.Infrastructure;

/// <summary>İstek boyunca seçili site. <see cref="Filters.SiteContextFilter"/> doldurur.</summary>
public interface ISiteContext
{
    /// <summary>Organizasyonda henüz site yoksa null.</summary>
    SiteListItemResponse? Site { get; }

    IReadOnlyList<SiteListItemResponse> Sites { get; }
}
