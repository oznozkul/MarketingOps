using Hangfire;
using MarketingOps.Application.Connectors.Commands.SyncGa4;
using MarketingOps.Application.Connectors.Commands.SyncSearchConsole;
using MarketingOps.Application.Connectors.Commands.SyncWebVitals;
using MarketingOps.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>
/// Günlük connector senkronu. Web Vitals ölçümü (Google sitenin sayfalarını açar) zamanlanmış olarak yalnızca sahipliği
/// doğrulanmış sitelerde yapılır; elle her zaman başlatılabilir.
/// </summary>
public sealed class ConnectorSyncJob(ISender sender, AppDbContext db)
{
    [AutomaticRetry(Attempts = 1)]
    public async Task RunAsync(Guid siteId, bool scheduled, CancellationToken cancellationToken)
    {
        var verified = await db.Sites.Where(s => s.Id == siteId).Select(s => (bool?)s.IsOwnershipVerified).FirstOrDefaultAsync(cancellationToken);
        if (verified is null) return;
        await sender.Send(new SyncSearchConsoleCommand(siteId), cancellationToken);
        await sender.Send(new SyncGa4Command(siteId), cancellationToken);
        if (scheduled && verified == true) await sender.Send(new SyncWebVitalsCommand(siteId), cancellationToken);
    }

    [AutomaticRetry(Attempts = 0)]
    public Task MeasureWebVitalsAsync(Guid siteId, CancellationToken cancellationToken) => sender.Send(new SyncWebVitalsCommand(siteId), cancellationToken);
}
