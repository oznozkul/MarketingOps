using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.WorkTasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Commands.RequestTaskEnrichment;

public sealed class RequestTaskEnrichmentCommandHandler(IAppDbContext db, ITaskJobScheduler jobs, IAiSuggestionService? ai = null)
    : IRequestHandler<RequestTaskEnrichmentCommand>
{
    public async Task Handle(RequestTaskEnrichmentCommand request, CancellationToken cancellationToken)
    {
        if (ai is null) throw new ConflictException("AI sağlayıcısı ayarlı değil. Öneri kural şablonundan gelir.");
        var status = await db.WorkTasks.Where(t => t.Id == request.TaskId && t.SiteId == request.SiteId).Select(t => (WorkTaskStatus?)t.Status)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Görev bulunamadı.");
        if (status == WorkTaskStatus.Closed) throw new ConflictException("Kapanmış görevin önerisi yeniden üretilmez.");
        jobs.EnqueueEnrichment(request.TaskId);
    }
}
