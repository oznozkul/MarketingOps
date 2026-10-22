using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateSite;

public sealed record UpdateSiteCommand(Guid SiteId, string Name) : IRequest;
