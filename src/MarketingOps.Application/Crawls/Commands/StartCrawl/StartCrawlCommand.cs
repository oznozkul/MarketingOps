using MarketingOps.Domain.Crawls;
using MediatR;

namespace MarketingOps.Application.Crawls.Commands.StartCrawl;

public sealed record StartCrawlCommand(Guid SiteId, CrawlTrigger TriggeredBy = CrawlTrigger.Manual) : IRequest<Guid>;
