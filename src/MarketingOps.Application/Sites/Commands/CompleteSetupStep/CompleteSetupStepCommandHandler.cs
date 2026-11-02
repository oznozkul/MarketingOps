using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Sites.Commands.CompleteSetupStep;

public sealed class CompleteSetupStepCommandHandler(IAppDbContext db) : IRequestHandler<CompleteSetupStepCommand>
{
    public async Task Handle(CompleteSetupStepCommand request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == request.SiteId, cancellationToken) ?? throw new NotFoundException("Site bulunamadı.");
        site.CompleteSetupStep(request.Step);
        await db.SaveChangesAsync(cancellationToken);
    }
}
