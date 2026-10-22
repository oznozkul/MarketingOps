using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Commands.RevertFalsePositive;

public sealed class RevertFalsePositiveCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<RevertFalsePositiveCommand>
{
    public async Task Handle(RevertFalsePositiveCommand request, CancellationToken cancellationToken)
    {
        var finding = await db.Findings.FirstOrDefaultAsync(f => f.Id == request.FindingId && f.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Bulgu bulunamadı.");
        finding.RevertFalsePositive(request.Actor, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
