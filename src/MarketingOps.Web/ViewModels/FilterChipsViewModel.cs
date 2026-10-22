namespace MarketingOps.Web.ViewModels;

public record FilterChipsViewModel(IReadOnlyList<ChipViewModel> Chips, string ClearUrl, string Target);
