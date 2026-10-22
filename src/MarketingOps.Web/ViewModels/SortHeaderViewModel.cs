namespace MarketingOps.Web.ViewModels;

public record SortHeaderViewModel(string Label, string Url, string? AriaSort, bool Active, bool Descending, string Target, string? CssClass, string? Show = null);
