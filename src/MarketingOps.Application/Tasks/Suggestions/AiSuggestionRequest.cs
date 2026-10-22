using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Suggestions;

/// <summary>
/// Modele verilen bağlam. Yalnızca bulgudaki gerçek veri ve kuralın şablonu verilir; sayılar buradan gelir,
/// model yeni sayı üretmemelidir. <see cref="Thresholds"/>: kuralın sitedeki geçerli eşikleri ("En uzun title: 60 karakter"). <see cref="ReferenceKey"/> bilgi tabanından ilgili parçaları seçmek için kullanılır.
/// </summary>
public sealed record AiSuggestionRequest(
    string RuleCode,
    string RuleName,
    string RuleDescription,
    string ReferenceKey,
    string? PageTypeName,
    int AffectedCount,
    string Problem,
    string TemplateImpact,
    SuggestionKind Kind,
    string TemplateSummary,
    string TemplateContent,
    IReadOnlyList<SuggestionExample> Examples,
    IReadOnlyList<string> Thresholds);
