using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.WorkTasks;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks;

/// <summary>
/// Bulgudan görev üretir (bulgu başına tek görev). Hem bulgu açıldığında hem de toplu "görev oluştur" işleminde kullanılır.
/// Kayıt çağırana aittir; böylece birden fazla görev tek seferde yazılabilir.
/// </summary>
public sealed class WorkTaskFactory(IAppDbContext db, TimeProvider clock)
{
    /// <summary>Bulgunun görevi zaten varsa null döner.</summary>
    public async Task<WorkTask?> CreateAsync(Finding finding, CancellationToken ct)
    {
        if (await db.WorkTasks.AnyAsync(t => t.FindingId == finding.Id, ct)
            || db.WorkTasks.Local.Any(t => t.FindingId == finding.Id)) return null;

        var rule = RuleCatalog.Find(finding.RuleCode);
        if (rule is null) return null;

        var site = await db.Sites.Include(s => s.PageTypes).FirstAsync(s => s.Id == finding.SiteId, ct);
        var pageTypeName = site.PageTypes.FirstOrDefault(p => p.Id == finding.PageTypeId)?.Name;
        var (number, key) = site.AllocateTaskKey();

        var task = WorkTask.Create(site.Id, finding.Id, number, key, TaskTemplate.Build(rule, finding, pageTypeName), clock.GetUtcNow());
        db.WorkTasks.Add(task);
        return task;
    }
}
