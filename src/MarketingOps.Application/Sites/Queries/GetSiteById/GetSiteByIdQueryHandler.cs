using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Sites;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Sites.Queries.GetSiteById;

public sealed class GetSiteByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetSiteByIdQuery, SiteDetailResponse>
{
    public async Task<SiteDetailResponse> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.AsNoTracking()
            .Where(s => s.Id == request.SiteId)
            .Select(s => new SiteDetailResponse(
                s.Id,
                s.Name,
                s.Domain,
                s.IsOwnershipVerified,
                s.CreatedAt,
                new CrawlSettingsResponse(
                    s.CrawlSettings.Schedule,
                    s.CrawlSettings.MaxRequestsPerSecond,
                    s.CrawlSettings.MaxPages,
                    s.CrawlSettings.ExcludedPathPatterns.ToList(),
                    true,
                    s.CrawlSettings.RenderEnabled,
                    s.CrawlSettings.RenderSamplesPerPageType),
                new ExpectedTrackingIdsResponse(
                    s.ExpectedTrackingIds.Ga4MeasurementId,
                    s.ExpectedTrackingIds.GtmContainerId,
                    s.ExpectedTrackingIds.MetaPixelId,
                    s.ExpectedTrackingIds.GoogleAdsConversionId),
                s.PageTypes
                    .OrderBy(p => p.Name)
                    .Select(p => new PageTypeResponse(
                        p.Id,
                        p.Name,
                        p.Mode,
                        p.Conditions.Select(c => new MatchConditionResponse(c.Type, c.Value)).ToList()))
                    .ToList(),
                s.TaskKeyPrefix,
                new TrackingJourneyResponse(s.Journey.CartUrl, s.Journey.CheckoutUrl, s.Journey.ThankYouUrl, s.Journey.ConsentButtonSelector),
                new OrderSourceResponse(s.OrderSource.ProtectedConnection != null && s.OrderSource.Query != null, s.OrderSource.Query)))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Site bulunamadı.");

        // Görev özelliğinden önce oluşturulmuş sitede önek ilk görevde addan türetilir; ekranda da o gösterilir.
        return site.TaskKeyPrefix.Length > 0 ? site : site with { TaskKeyPrefix = Site.DerivePrefix(site.Name) };
    }
}
