using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.PageTypes.Commands.AddPageType;

public sealed record AddPageTypeCommand(Guid SiteId, string Name, MatchMode Mode, IReadOnlyList<MatchConditionInput> Conditions) : IRequest<Guid>;
