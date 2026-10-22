namespace MarketingOps.Web.ViewModels;

public record PaginationViewModel(int Total, int FirstIndex, int LastIndex, int PageSize, string? PreviousUrl, string? NextUrl, IReadOnlyList<PageLink> Pages, string PageSizeUrl, string Target, string ItemLabel);
