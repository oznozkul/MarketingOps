using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Sites;
using MediatR;

namespace MarketingOps.Application.PageTypes.Commands.UpdatePageType;

public sealed class UpdatePageTypeCommandHandler(IAppDbContext db) : IRequestHandler<UpdatePageTypeCommand>
{
    public async Task Handle(UpdatePageTypeCommand request, CancellationToken cancellationToken)
    {
        var site = await db.LoadAsync(request.SiteId, cancellationToken);
        if (site.PageTypes.All(p => p.Id != request.PageTypeId))
            throw new NotFoundException("Sayfa tipi bulunamadı.");

        site.UpdatePageType(request.PageTypeId, request.Name, request.Mode, request.Conditions.Select(c => c.ToDomain()));
        await db.SaveChangesAsync(cancellationToken);
    }
}
