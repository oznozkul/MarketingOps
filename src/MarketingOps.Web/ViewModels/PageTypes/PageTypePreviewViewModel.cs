using MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;

namespace MarketingOps.Web.ViewModels.PageTypes;

/// <summary>Koşullar henüz tamamlanmamışsa <see cref="Result"/> null'dır ve <see cref="Problems"/> nedenini söyler.</summary>
public sealed record PageTypePreviewViewModel(PageTypeMatchPreviewResponse? Result, IReadOnlyList<string> Problems);
