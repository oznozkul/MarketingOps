using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateExpectedTrackingIds;

public sealed class UpdateExpectedTrackingIdsCommandHandler(IAppDbContext db) : IRequestHandler<UpdateExpectedTrackingIdsCommand>
{
    public async Task Handle(UpdateExpectedTrackingIdsCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        site.UpdateExpectedTrackingIds(new ExpectedTrackingIds(
            request.Ga4MeasurementId, request.GtmContainerId, request.MetaPixelId, request.GoogleAdsConversionId));
        await db.SaveChangesAsync(cancellationToken);
    }
}
