using MediatR;

namespace MarketingOps.Application.Sites.Commands.DeleteSite;

public sealed record DeleteSiteCommand(Guid SiteId) : IRequest;
