using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Commands.BulkUpdateFindings;

public sealed class BulkUpdateFindingsCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<BulkUpdateFindingsCommand, BulkUpdateResult>
{
    public async Task<BulkUpdateResult> Handle(BulkUpdateFindingsCommand request, CancellationToken cancellationToken)
    {
        var findings = await db.Findings.Where(f => f.SiteId == request.SiteId && request.FindingIds.Contains(f.Id)).ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var updated = 0;
        foreach (var finding in findings)
        {
            try
            {
                if (request.Action == BulkFindingAction.Snooze) finding.Snooze(SnoozeDate.ToInstant(request.Until!.Value), request.Actor, now);
                else finding.MarkFalsePositive(request.Reason!, request.Actor, now);
                updated++;
            }
            catch (DomainException)
            {
                // Durumu bu işleme uygun olmayan bulgu (ör. zaten yanlış alarm ya da çözülmüş) atlanır.
            }
        }

        if (updated > 0) await db.SaveChangesAsync(cancellationToken);
        return new BulkUpdateResult(updated, request.FindingIds.Count - updated);
    }
}
