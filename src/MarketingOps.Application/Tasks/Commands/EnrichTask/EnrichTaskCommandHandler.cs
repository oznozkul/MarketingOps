using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Rules;
using MarketingOps.Application.Tasks.Suggestions;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.WorkTasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.Tasks.Commands.EnrichTask;

public sealed class EnrichTaskCommandHandler(IAppDbContext db, TimeProvider clock, ILogger<EnrichTaskCommandHandler> logger, IAiSuggestionService? ai = null)
    : IRequestHandler<EnrichTaskCommand>
{
    private static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    public async Task Handle(EnrichTaskCommand request, CancellationToken cancellationToken)
    {
        if (ai is null) return;
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        if (task is null || task.Status == WorkTaskStatus.Closed) return;

        var finding = await db.Findings.AsNoTracking().FirstAsync(f => f.Id == task.FindingId, cancellationToken);
        var rule = RuleCatalog.Find(finding.RuleCode);
        if (rule is null) return;
        var pageTypeName = await db.Sites.Where(s => s.Id == task.SiteId).SelectMany(s => s.PageTypes)
            .Where(p => p.Id == finding.PageTypeId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken);

        // Her zaman şablondan başlanır: tekrar üretimde önceki AI metni bağlam olarak verilmez.
        var template = TaskTemplate.Build(rule, finding, pageTypeName);
        // Model eşiği bilmezse uydurur; geçerli eşikler gerçek veri olarak verilir.
        var thresholds = (await db.LoadAsync(task.SiteId, cancellationToken)).ThresholdsOf(rule);
        var thresholdTexts = rule.Thresholds
            .Select(t => $"{t.Label}: {thresholds.GetValueOrDefault(t.Key, t.Default).ToString("0.##", Tr)} {t.Unit}")
            .ToList();
        var result = await ai.SuggestAsync(new AiSuggestionRequest(
            rule.Code, rule.Name, rule.Description, rule.ReferenceKey, pageTypeName, finding.AffectedCount, template.Problem,
            template.Impact, template.Suggestion.Kind, template.Suggestion.Summary, template.Suggestion.Content, template.Suggestion.Examples, thresholdTexts),
            cancellationToken);

        var suggested = result.SuggestedValues
            .GroupBy(v => v.Url, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Suggested, StringComparer.OrdinalIgnoreCase);
        // Örnek tablo şablondan gelir; model yalnızca kendisine verilen URL'ler için önerilen değer ekleyebilir.
        // Mevcut değerin aynısı öneri değildir (küçük modeller sık sık metni olduğu gibi geri verir).
        var examples = template.Suggestion.Examples
            .Select(e => e with { Suggested = suggested.GetValueOrDefault(e.Url) is { } value && !SameAs(value, e) ? value : null })
            .ToList();
        var suggestion = new TaskSuggestion(template.Suggestion.Kind, Fallback(result.Summary, template.Suggestion.Summary),
            Fallback(result.Content, template.Suggestion.Content), examples, ai.ModelName);

        task.ApplyAiSuggestion(result.Impact, suggestion, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Görev {TaskKey} önerisi {Model} ile zenginleştirildi.", task.Key, ai.ModelName);
    }

    private static bool SameAs(string value, SuggestionExample example) =>
        string.Equals(value.Trim(), example.Detail?.Trim(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(value.Trim(), example.Current?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string Fallback(string value, string template) => string.IsNullOrWhiteSpace(value) ? template : value.Trim();
}
