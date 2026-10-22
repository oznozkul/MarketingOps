using System.Globalization;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;

namespace MarketingOps.Domain.WorkTasks;

/// <summary>
/// Görevin AI'sız içeriği: kuralın şablonu + bulgunun somut değerleri. AI hiç çalışmasa da görev bununla tamdır;
/// sayılar her zaman buradan gelir.
/// </summary>
public static class TaskTemplate
{
    public const int ExampleCount = 10;
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static TaskDraft Build(IRule rule, Finding finding, string? pageTypeName)
    {
        var suggestion = rule.Suggestion;
        var examples = finding.Occurrences.Take(ExampleCount).Select(o => new SuggestionExample(o.Url, o.CurrentValue, null)).ToList();
        return new TaskDraft(
            finding.Title,
            Problem(finding.AffectedCount, pageTypeName, finding.Occurrences.FirstOrDefault()),
            rule.Impact,
            new TaskSuggestion(suggestion.Kind, suggestion.Summary, suggestion.Content, examples),
            [rule.ReferenceKey],
            rule.Team,
            [(AutomaticCriterion(rule, pageTypeName), VerificationType.Automatic), (rule.TeamCriterion, VerificationType.TeamConfirmation)]);
    }

    /// <summary>Sorunun somut anlatımı: hangi sayfa grubunda kaç sayfa ve bir örnek.</summary>
    public static string Problem(int affectedCount, string? pageTypeName, FindingOccurrence? sample)
    {
        var group = pageTypeName is null ? "Hiçbir sayfa tipine uymayan sayfalardan" : $"{pageTypeName} sayfalarından";
        var text = $"{group} {affectedCount.ToString("N0", Tr)} tanesi etkileniyor.";
        if (sample is not null)
            text += $" Örnek: {sample.Url}" + (sample.CurrentValue is null ? "" : $" — mevcut değer: {sample.CurrentValue}") + ".";
        return text;
    }

    private static string AutomaticCriterion(IRule rule, string? pageTypeName) =>
        pageTypeName is null
            ? $"Sonraki taramada \"{rule.Name}\" kontrolü bu grupta sorun bulmuyor."
            : $"Sonraki taramada {pageTypeName} sayfalarında \"{rule.Name}\" kontrolü sorun bulmuyor.";
}
