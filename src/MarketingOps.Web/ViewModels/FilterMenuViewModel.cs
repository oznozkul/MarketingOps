namespace MarketingOps.Web.ViewModels;

/// <summary>Açılır onay kutusu listesi: bir query anahtarı (<see cref="Name"/>) için çoklu seçim.</summary>
public sealed record FilterMenuViewModel(string Id, string Label, string Name, IReadOnlyList<FilterOption> Options, string Form)
{
    public int SelectedCount => Options.Count(o => o.Selected);
}
