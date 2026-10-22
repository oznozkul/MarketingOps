using MarketingOps.Application.Common.Interfaces;
using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateSite;

public sealed class UpdateSiteCommandHandler(IAppDbContext db) : IRequestHandler<UpdateSiteCommand>
{
    public async Task Handle(UpdateSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        site.Rename(request.Name);
        await db.SaveChangesAsync(cancellationToken);
    }
}
