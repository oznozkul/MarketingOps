using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules;

/// <summary>Bir kontrolün tanımı. Kurallar kodda tanımlanır; site bazlı ayarları <see cref="SiteRuleSetting"/>'tedir.</summary>
public interface IRule
{
    string Code { get; }
    string Name { get; }
    string Description { get; }
    FindingCategory Category { get; }
    Severity DefaultSeverity { get; }
    RuleScope Scope { get; }
    IReadOnlyList<RuleThreshold> Thresholds { get; }

    /// <summary>Önerilerde kullanılacak kaynak (ör. "perf-doc:p30"); metinler Faz 4'te bağlanır.</summary>
    string ReferenceKey { get; }

    /// <summary>Sorunun neden önemli olduğu. Veriye dayanmayan genel açıklama; sayı içermez.</summary>
    string Impact { get; }

    /// <summary>Görevin varsayılan olarak atandığı ekip.</summary>
    Team Team { get; }

    /// <summary>AI'sız önerilen çözüm; görev oluşturulurken bulgunun somut değerleriyle birleşir.</summary>
    SuggestionTemplate Suggestion { get; }

    /// <summary>Taramayla doğrulanamayan, ekibin onaylayacağı kabul kriteri.</summary>
    string TeamCriterion { get; }

    /// <summary>Bulgu başlığı, ör. "340 sayfada title tekrarlanıyor".</summary>
    string Title(int affectedCount);
}
