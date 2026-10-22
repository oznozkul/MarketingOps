using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Commands.SnoozeFinding;

public sealed class SnoozeFindingCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<SnoozeFindingCommand>
{
    public async Task Handle(SnoozeFindingCommand request, CancellationToken cancellationToken)
    {
        var finding = await db.Findings.FirstOrDefaultAsync(f => f.Id == request.FindingId && f.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Bulgu bulunamadı.");
        finding.Snooze(SnoozeDate.ToInstant(request.Until), request.Actor, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
