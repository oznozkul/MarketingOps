using MediatR;

namespace MarketingOps.Application.Sites.Commands.CreateSite;

public sealed record CreateSiteCommand(string Name, string Domain) : IRequest<Guid>;
