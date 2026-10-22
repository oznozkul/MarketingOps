using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Sites;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;

public sealed class PreviewPageTypeMatchQueryHandler(IAppDbContext db) : IRequestHandler<PreviewPageTypeMatchQuery, PageTypeMatchPreviewResponse>
{
    public async Task<PageTypeMatchPreviewResponse> Handle(PreviewPageTypeMatchQuery request, CancellationToken cancellationToken)
    {
        // Koşullar domain nesnesine çevrilirken doğrulanır (ör. geçersiz regex → DomainException).
        var conditions = request.Conditions.Select(c => c.ToDomain()).ToList();

        // Çakışma için sitenin kayıtlı tiplerinin eşleşme kuralları gerekir; bu yüzden aggregate okunur (yazılmaz).
        Site? site = null;
        if (request.SiteId is { } siteId)
            site = await db.Sites.AsNoTracking().Include(s => s.PageTypes).FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken)
                ?? throw new NotFoundException("Site bulunamadı.");

        var results = request.Samples.Select(sample =>
        {
            var url = new Uri(sample.Url);
            var schemaTypes = sample.SchemaTypes ?? [];
            var isMatch = PageType.Evaluate(request.Mode, conditions, url, schemaTypes);
            IReadOnlyList<string> overlaps = isMatch && site is not null
                ? site.FindMatchingPageTypes(url, schemaTypes, request.PageTypeId).Select(p => p.Name).ToList()
                : [];
            return new UrlMatchResponse(sample.Url, isMatch, overlaps);
        }).ToList();

        return new PageTypeMatchPreviewResponse(
            results.Count,
            results.Count(r => r.IsMatch),
            results.Count(r => r.OverlappingPageTypes.Count > 0),
            results);
    }
}
