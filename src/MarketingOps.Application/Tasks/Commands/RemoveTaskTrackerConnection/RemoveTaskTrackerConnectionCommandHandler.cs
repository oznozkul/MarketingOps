using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Commands.RemoveTaskTrackerConnection;

public sealed class RemoveTaskTrackerConnectionCommandHandler(IAppDbContext db) : IRequestHandler<RemoveTaskTrackerConnectionCommand>
{
    public async Task Handle(RemoveTaskTrackerConnectionCommand request, CancellationToken cancellationToken)
    {
        var connection = await db.TaskTrackerConnections.FirstOrDefaultAsync(c => c.SiteId == request.SiteId, cancellationToken);
        if (connection is null) return;
        db.TaskTrackerConnections.Remove(connection);
        await db.SaveChangesAsync(cancellationToken);
    }
}
