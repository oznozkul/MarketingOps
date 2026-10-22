using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateCrawlSettings;

public sealed class UpdateCrawlSettingsCommandHandler(IAppDbContext db, ICrawlScheduler scheduler) : IRequestHandler<UpdateCrawlSettingsCommand>
{
    public async Task Handle(UpdateCrawlSettingsCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        site.UpdateCrawlSettings(new CrawlSettings(request.Schedule, request.MaxRequestsPerSecond, request.MaxPages, request.ExcludedPathPatterns));
        await db.SaveChangesAsync(cancellationToken);
        scheduler.ScheduleSite(site.Id, site.CrawlSettings.Schedule);
    }
}
