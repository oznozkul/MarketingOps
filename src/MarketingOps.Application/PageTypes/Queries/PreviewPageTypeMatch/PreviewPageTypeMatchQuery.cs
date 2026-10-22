using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;

/// <summary>
/// Kaydetmeden, verilen koşulların örnek URL'lerden hangileriyle eşleştiğini gösterir.
/// <see cref="SiteId"/> verilirse aynı URL'e uyan diğer sayfa tipleri çakışma olarak raporlanır;
/// <see cref="PageTypeId"/> düzenlenen tipin kendisiyle çakışmasını önler.
/// </summary>
public sealed record PreviewPageTypeMatchQuery(
    MatchMode Mode,
    IReadOnlyList<MatchConditionInput> Conditions,
    IReadOnlyList<PreviewSampleInput> Samples,
    Guid? SiteId = null,
    Guid? PageTypeId = null) : IRequest<PageTypeMatchPreviewResponse>;
