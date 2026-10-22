using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Sites;
using MediatR;

namespace MarketingOps.Application.PageTypes.Commands.AddPageType;

public sealed class AddPageTypeCommandHandler(IAppDbContext db) : IRequestHandler<AddPageTypeCommand, Guid>
{
    public async Task<Guid> Handle(AddPageTypeCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        var pageType = site.AddPageType(request.Name, request.Mode, request.Conditions.Select(c => c.ToDomain()));
        await db.SaveChangesAsync(cancellationToken);
        return pageType.Id;
    }
}
