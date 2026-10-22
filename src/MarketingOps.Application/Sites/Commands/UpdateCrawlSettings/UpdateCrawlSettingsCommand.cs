using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateCrawlSettings;

public sealed record UpdateCrawlSettingsCommand(
    Guid SiteId,
    CrawlSchedule Schedule,
    int MaxRequestsPerSecond,
    int MaxPages,
    IReadOnlyList<string> ExcludedPathPatterns) : IRequest;
