using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Notifications;
using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Scores;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Scores.Commands.RecordScoreSnapshots;

public sealed class RecordScoreSnapshotsCommandHandler(IAppDbContext db, ScoreReader scores, INotificationJobScheduler jobs, TimeProvider clock)
    : IRequestHandler<RecordScoreSnapshotsCommand>
{
    public async Task Handle(RecordScoreSnapshotsCommand request, CancellationToken cancellationToken)
    {
        var facts = await scores.LoadAsync(request.SiteId, cancellationToken);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var now = clock.GetUtcNow();
        var existing = await db.ScoreSnapshots.Where(s => s.SiteId == request.SiteId && s.Date == today).ToListAsync(cancellationToken);
        var previous = await db.ScoreSnapshots.AsNoTracking()
            .Where(s => s.SiteId == request.SiteId && s.Date < today)
            .ToListAsync(cancellationToken);
        var before = previous.GroupBy(s => s.Category).ToDictionary(g => g.Key, g => g.MaxBy(s => s.Date)!.Score);
        var drops = new List<(ScoreCategory Category, int Drop)>();

        foreach (var category in Enum.GetValues<ScoreCategory>())
        {
            var score = ScoreCalculator.Score(ScoreReader.Inputs(facts, category));
            var row = existing.FirstOrDefault(s => s.Category == category);
            if (row is null) db.ScoreSnapshots.Add(ScoreSnapshot.Create(request.SiteId, category, score, today, now));
            else row.Replace(score, now);

            if (before.TryGetValue(category, out var earlier) && earlier - score > 0)
                drops.Add((category, earlier - score));
        }

        await db.SaveChangesAsync(cancellationToken);
        foreach (var (category, drop) in drops)
            jobs.Enqueue(request.SiteId, NotificationEventType.ScoreDropped, null, drop, category.ToString());
    }
}
