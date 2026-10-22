using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Sites;
using MediatR;

namespace MarketingOps.Application.PageTypes.Commands.RemovePageType;

public sealed class RemovePageTypeCommandHandler(IAppDbContext db) : IRequestHandler<RemovePageTypeCommand>
{
    public async Task Handle(RemovePageTypeCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        if (site.PageTypes.All(p => p.Id != request.PageTypeId))
            throw new NotFoundException("Sayfa tipi bulunamadı.");

        site.RemovePageType(request.PageTypeId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
