using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.PageTypes.Commands.UpdatePageType;

public sealed record UpdatePageTypeCommand(Guid SiteId, Guid PageTypeId, string Name, MatchMode Mode, IReadOnlyList<MatchConditionInput> Conditions) : IRequest;
