using MarketingOps.Domain.Sites;
using MediatR;

namespace MarketingOps.Application.Sites.Commands.CompleteSetupStep;

public sealed record CompleteSetupStepCommand(Guid SiteId, SiteSetupStep Step) : IRequest;
