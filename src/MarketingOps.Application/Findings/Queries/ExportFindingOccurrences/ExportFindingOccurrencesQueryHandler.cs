using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Findings.Queries.GetFindingById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Queries.ExportFindingOccurrences;

public sealed class ExportFindingOccurrencesQueryHandler(IAppDbContext db) : IRequestHandler<ExportFindingOccurrencesQuery, IReadOnlyList<FindingOccurrenceResponse>>
{
    public async Task<IReadOnlyList<FindingOccurrenceResponse>> Handle(ExportFindingOccurrencesQuery request, CancellationToken cancellationToken)
    {
        var occurrences = await db.Findings.AsNoTracking()
            .Where(f => f.Id == request.FindingId && f.SiteId == request.SiteId)
            .Select(f => f.Occurrences)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Bulgu bulunamadı.");
        return occurrences.Select(o => new FindingOccurrenceResponse(o.Url, o.CurrentValue, o.Detail, o.Devices)).ToList();
    }
}
