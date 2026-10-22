using System.Globalization;
using MarketingOps.Application.Rules.Queries.GetRule;
using MarketingOps.Domain.Findings;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MarketingOps.Web.ViewModels.Rules;

/// <summary>
/// Kural ayarları formu. Eşikler metin olarak alınır ve kültürden bağımsız ayrıştırılır: tr-TR'de "1.5" ondalık sayılmaz,
/// tarayıcının sayı alanı ise her zaman noktalı gönderir.
/// </summary>
public sealed class RuleSettingsForm
{
    public bool IsEnabled { get; set; }
    public Severity? SeverityOverride { get; set; }
    public Dictionary<Guid, Severity?> PageTypeSeverities { get; set; } = [];
    public Dictionary<string, string?> Thresholds { get; set; } = [];

    public static RuleSettingsForm From(RuleDetailResponse detail) => new()
    {
        IsEnabled = detail.Rule.IsEnabled,
        SeverityOverride = detail.SeverityOverride,
        PageTypeSeverities = detail.PageTypes.ToDictionary(p => p.Id, p => p.SeverityOverride),
        Thresholds = detail.Thresholds.ToDictionary(t => t.Key, t => (string?)t.Value.ToString("0.##", CultureInfo.InvariantCulture))
    };

    /// <summary>Boş bırakılan eşik varsayılana döner (gönderilmez); sayı olmayan değer alan hatası olur.</summary>
    public Dictionary<string, decimal> ParseThresholds(ModelStateDictionary modelState)
    {
        var parsed = new Dictionary<string, decimal>();
        foreach (var (key, raw) in Thresholds)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (decimal.TryParse(raw.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) parsed[key] = value;
            else modelState.AddModelError($"Thresholds[{key}]", "Sayı girin.");
        }
        return parsed;
    }

    public Dictionary<Guid, Severity> ParsePageTypeSeverities() =>
        PageTypeSeverities.Where(p => p.Value is not null).ToDictionary(p => p.Key, p => p.Value!.Value);
}
