using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Sites.Commands.UpdateTaskKeyPrefix;

public sealed class UpdateTaskKeyPrefixCommandHandler(IAppDbContext db) : IRequestHandler<UpdateTaskKeyPrefixCommand>
{
    public async Task Handle(UpdateTaskKeyPrefixCommand request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == request.SiteId, cancellationToken) ?? throw new NotFoundException("Site bulunamadı.");
        site.UpdateTaskKeyPrefix(request.Prefix);
        await db.SaveChangesAsync(cancellationToken);
    }
}
