using MediatR;

namespace MarketingOps.Application.PageTypes.Commands.RemovePageType;

public sealed record RemovePageTypeCommand(Guid SiteId, Guid PageTypeId) : IRequest;
