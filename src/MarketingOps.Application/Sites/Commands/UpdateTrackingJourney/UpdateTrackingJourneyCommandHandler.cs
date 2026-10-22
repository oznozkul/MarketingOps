using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateTrackingJourney;

public sealed class UpdateTrackingJourneyCommandHandler(IAppDbContext db) : IRequestHandler<UpdateTrackingJourneyCommand>
{
    public async Task Handle(UpdateTrackingJourneyCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        site.UpdateTrackingJourney(new TrackingJourney(request.CartUrl, request.CheckoutUrl, request.ThankYouUrl, request.ConsentButtonSelector));
        await db.SaveChangesAsync(cancellationToken);
    }
}
