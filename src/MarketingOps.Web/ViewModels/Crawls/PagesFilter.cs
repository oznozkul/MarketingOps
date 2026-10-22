using MarketingOps.Application.Crawls.Queries.GetPages;

namespace MarketingOps.Web.ViewModels.Crawls;

/// <summary>Sayfa gezgininin query string'de tutulan filtreleri.</summary>
public sealed record PagesFilter(Guid? PageTypeId, bool Unmatched, PageStatusFilter? Status, string? Q)
{
    public bool IsEmpty => PageTypeId is null && !Unmatched && Status is null && string.IsNullOrWhiteSpace(Q);
}
