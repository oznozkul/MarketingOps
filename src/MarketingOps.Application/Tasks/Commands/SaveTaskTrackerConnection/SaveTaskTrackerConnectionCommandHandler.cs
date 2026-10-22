using FluentValidation;
using FluentValidation.Results;
using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Tasks.Tracking;
using MarketingOps.Domain.Connectors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Commands.SaveTaskTrackerConnection;

public sealed class SaveTaskTrackerConnectionCommandHandler(IAppDbContext db, TaskTrackerTokenProtector protector, TimeProvider clock)
    : IRequestHandler<SaveTaskTrackerConnectionCommand, Guid>
{
    public async Task<Guid> Handle(SaveTaskTrackerConnectionCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken)) throw new NotFoundException("Site bulunamadı.");
        var token = string.IsNullOrWhiteSpace(request.ApiToken) ? null : protector.Protect(request.ApiToken.Trim());
        var connection = await db.TaskTrackerConnections.FirstOrDefaultAsync(c => c.SiteId == request.SiteId, cancellationToken);
        if (connection is null)
        {
            if (token is null) throw new ValidationException([new ValidationFailure(nameof(request.ApiToken), "API anahtarı zorunludur.")]);
            connection = TaskTrackerConnection.Create(request.SiteId, request.BaseUrl, request.ProjectKey, request.IssueType, request.Email, token, clock.GetUtcNow());
            db.TaskTrackerConnections.Add(connection);
        }
        else
        {
            connection.Update(request.BaseUrl, request.ProjectKey, request.IssueType, request.Email, token, clock.GetUtcNow());
        }
        await db.SaveChangesAsync(cancellationToken);
        return connection.Id;
    }
}
