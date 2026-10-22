using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Commands.MarkFindingFalsePositive;

public sealed class MarkFindingFalsePositiveCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<MarkFindingFalsePositiveCommand>
{
    public async Task Handle(MarkFindingFalsePositiveCommand request, CancellationToken cancellationToken)
    {
        var finding = await db.Findings.FirstOrDefaultAsync(f => f.Id == request.FindingId && f.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Bulgu bulunamadı.");
        finding.MarkFalsePositive(request.Reason, request.Actor, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
