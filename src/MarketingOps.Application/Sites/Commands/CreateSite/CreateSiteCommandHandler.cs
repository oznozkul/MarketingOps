using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Organizations;
using MarketingOps.Domain.Sites;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Sites.Commands.CreateSite;

public sealed class CreateSiteCommandHandler(IAppDbContext db, ICrawlScheduler scheduler, TimeProvider clock) : IRequestHandler<CreateSiteCommand, Guid>
{
    public async Task<Guid> Handle(CreateSiteCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await DefaultOrganization.GetIdAsync(db, cancellationToken);
        var site = Site.Create(organizationId, request.Name, request.Domain, clock.GetUtcNow());

        var exists = await db.Sites.AnyAsync(s => s.OrganizationId == organizationId && s.Domain == site.Domain, cancellationToken);
        if (exists) throw new ConflictException($"{site.Domain} için zaten bir site kayıtlı.");

        db.Sites.Add(site);
        await db.SaveChangesAsync(cancellationToken);
        scheduler.ScheduleSite(site.Id, site.CrawlSettings.Schedule);
        return site.Id;
    }
}
