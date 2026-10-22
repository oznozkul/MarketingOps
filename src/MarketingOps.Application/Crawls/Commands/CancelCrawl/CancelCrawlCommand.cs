using MediatR;

namespace MarketingOps.Application.Crawls.Commands.CancelCrawl;

public sealed record CancelCrawlCommand(Guid SiteId, Guid CrawlId) : IRequest;
