using System.Text;
using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Tasks.Tracking;
using MarketingOps.Domain.WorkTasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Commands.ExportTasks;

public sealed class ExportTasksCommandHandler(IAppDbContext db, ITaskTracker tracker, TaskTrackerTokenProtector protector, TimeProvider clock)
    : IRequestHandler<ExportTasksCommand, ExportTasksResult>
{
    public async Task<ExportTasksResult> Handle(ExportTasksCommand request, CancellationToken cancellationToken)
    {
        var connection = await db.TaskTrackerConnections.AsNoTracking().FirstOrDefaultAsync(c => c.SiteId == request.SiteId, cancellationToken)
            ?? throw new ConflictException("Görev sistemi bağlı değil. Ayarlar > Siteler > Bağlantılar'dan Jira bağlantısını kurun.");
        var credentials = new TaskTrackerCredentials(connection.BaseUrl, connection.Email, protector.Unprotect(connection.ProtectedApiToken),
            connection.ProjectKey, connection.IssueType);

        var findingIds = request.FindingIds ?? [];
        var tasks = await db.WorkTasks
            .Where(t => t.SiteId == request.SiteId && (request.TaskIds.Contains(t.Id) || findingIds.Contains(t.FindingId)))
            .ToListAsync(cancellationToken);
        var errors = new List<string>();
        var exported = 0;
        foreach (var task in tasks)
        {
            try
            {
                var issue = await tracker.UpsertAsync(credentials,
                    new ExternalIssueRequest(task.ExternalTrackerKey, $"[{task.Key}] {task.Title}", Description(task), ["marketingops", task.AssignedTeam.ToString().ToLowerInvariant()]),
                    cancellationToken);
                task.LinkExternal(issue.Key, issue.Url, clock.GetUtcNow());
                exported++;
            }
            catch (TaskTrackerException ex)
            {
                errors.Add($"{task.Key}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ExportTasksResult(exported, errors);
    }

    /// <summary>Jira açıklaması: geliştiricinin başka yere bakmadan işi yapabileceği bütün içerik.</summary>
    private static string Description(WorkTask task)
    {
        var text = new StringBuilder()
            .AppendLine("Problem").AppendLine(task.Problem).AppendLine()
            .AppendLine("Etki").AppendLine(task.Impact).AppendLine()
            .AppendLine("Önerilen çözüm" + (task.Suggestion.Model is null ? "" : $" (AI tarafından üretildi: {task.Suggestion.Model}; uygulamayı ekip yapar)"))
            .AppendLine(task.Suggestion.Summary).AppendLine(task.Suggestion.Content).AppendLine();
        if (task.Suggestion.Examples.Count > 0)
        {
            text.AppendLine("Örnek sayfalar");
            foreach (var e in task.Suggestion.Examples)
                text.AppendLine($"- {e.Url}" + (e.Current is null ? "" : $" | mevcut: {e.Current}") + (e.Suggested is null ? "" : $" | önerilen: {e.Suggested}"));
            text.AppendLine();
        }
        text.AppendLine("Kabul kriterleri");
        foreach (var c in task.Criteria)
            text.AppendLine($"- [{(c.IsMet ? "x" : " ")}] {c.Text} ({(c.VerificationType == VerificationType.Automatic ? "Otomatik · sonraki tarama" : "Ekip onayı")})");
        return text.ToString();
    }
}
