using MarketingOps.Application.Common.Interfaces;
using MediatR;

namespace MarketingOps.Application.Sites.Commands.DeleteSite;

public sealed class DeleteSiteCommandHandler(IAppDbContext db, ICrawlScheduler scheduler) : IRequestHandler<DeleteSiteCommand>
{
    public async Task Handle(DeleteSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        db.Sites.Remove(site);
        await db.SaveChangesAsync(cancellationToken);
        scheduler.RemoveSite(site.Id);
    }
}
